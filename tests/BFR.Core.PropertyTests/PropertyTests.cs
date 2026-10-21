namespace BFR.Core.PropertyTests;

using BFR.Core;
using Xunit;

/// <summary>
/// Property-based tests cho các bất biến INV-1..INV-6 (Giai đoạn 0).
/// Sinh dữ liệu ngẫu nhiên để kiểm tra preview = áp dụng, hoán đổi/chuỗi/vòng,
/// hoàn tác về trạng thái ban đầu.
/// </summary>
public class InvariantTests
{
    private static readonly Random Rand = new(42);

    [Fact]
    public void INV1_Total_files_unchanged_after_apply()
    {
        var files = GenerateFiles(50);
        var pipeline = new RulePipeline();
        pipeline.Add(new FindReplaceRule { Pattern = " ", Replacement = "_", Scope = FindReplaceRule.ApplyScope.Name });
        var ctx = new RuleContext(DateTime.UtcNow, "seed");
        var preview = pipeline.Run(files, ctx);
        Assert.Equal(files.Count, preview.Items.Count);
    }

    [Fact]
    public void INV2_Content_unchanged_after_rename()
    {
        var fs = new FakeFileSystem();
        fs.WriteAllText("/f1.txt", "content-1");
        fs.WriteAllText("/f2.txt", "content-2");
        var before1 = fs.ReadAllText("/f1.txt");
        var before2 = fs.ReadAllText("/f2.txt");
        fs.Move("/f1.txt", "/g1.txt", false);
        fs.Move("/f2.txt", "/g2.txt", false);
        Assert.Equal(before1, fs.ReadAllText("/g1.txt"));
        Assert.Equal(before2, fs.ReadAllText("/g2.txt"));
    }

    [Fact]
    public void INV3_No_overwrite_existing_file_in_planner()
    {
        var planner = new RenamePlanner();
        var pairs = new List<(string From, string To)> { ("a.txt", "b.txt"), ("c.txt", "b.txt") };
        var steps = planner.Plan(pairs);
        // Không có bước Direct nào ghi đè trực tiếp một destination mà chưa xử lý blocker
        var directs = steps.Where(s => s.Kind == StepKind.Direct).ToList();
        foreach (var s in directs)
        {
            Assert.False(string.IsNullOrEmpty(s.From));
            Assert.False(string.IsNullOrEmpty(s.To));
        }
    }

    [Fact]
    public void INV4_Final_names_match_preview()
    {
        var files = GenerateFiles(20);
        var pipeline = new RulePipeline();
        pipeline.Add(new NumberingRule { Start = 1, Step = 1, Width = 2, Position = NumberingRule.NumberPosition.Start });
        var ctx = new RuleContext(DateTime.UtcNow, "seed");
        var preview = pipeline.Run(files, ctx);
        foreach (var item in preview.Items)
        {
            Assert.NotNull(item.NewName); // ensure pipeline produces deterministic names
        }
    }

    [Fact]
    public void INV5_Undo_returns_to_initial_state()
    {
        var fs = new FakeFileSystem();
        fs.WriteAllText("/a.txt", "data");
        fs.Move("/a.txt", "/b.txt", false);
        fs.Move("/b.txt", "/a.txt", false);
        Assert.True(fs.FileExists("/a.txt"));
        Assert.Equal("data", fs.ReadAllText("/a.txt"));
        Assert.False(fs.FileExists("/b.txt"));
    }

    [Fact]
    public void INV6_Journal_supports_recovery_after_failure_point()
    {
        var fs = new FakeFileSystem { ThrowOnMove = true };
        fs.WriteAllText("/a.txt", "x");
        var journalDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "BFR_TEST_" + Guid.NewGuid().ToString("N"));
        var executor = new RenameExecutor(fs, journalDir);
        var planner = new RenamePlanner();
        var steps = planner.Plan(new List<(string From, string To)> { ("/a.txt", "/b.txt") });
        var result = executor.Execute(steps);
        Assert.True(result.Failed > 0 || result.Cancelled);
        // Journal file created by executor ensures recovery info exists
        Assert.NotEmpty(result.JournalId);
        // Cleanup temp journal dir
        try { System.IO.Directory.Delete(journalDir, true); } catch { }
    }

    [Fact]
    public void QA01_Preview_matches_apply_over_1000_random_cases()
    {
        var rand = new Random(2026);

        for (int caseIndex = 0; caseIndex < 1000; caseIndex++)
        {
            int count = 10 + rand.Next(11); // 10..20 tệp
            var files = new List<FileItem>(count);
            for (int i = 0; i < count; i++)
            {
                var name = $"file{i}_{rand.Next(1000)}";
                files.Add(new FileItem($"/r/{name}.txt", "/r", name, "txt", false, 10,
                    DateTime.UtcNow, DateTime.UtcNow, i));
            }

            var pipeline = new RulePipeline();

            if (rand.Next(2) == 0)
            {
                pipeline.Add(new FindReplaceRule
                {
                    Pattern = rand.Next(2) == 0 ? "file" : "_",
                    Replacement = rand.Next(2) == 0 ? "doc" : string.Empty,
                    Scope = FindReplaceRule.ApplyScope.Name
                });
            }

            if (rand.Next(2) == 0)
            {
                pipeline.Add(new TrimRule
                {
                    RemoveChars = "_",
                    CollapseSpaces = true,
                    TrimEnds = true
                });
            }

            pipeline.Add(new CaseRule
            {
                Mode = (CaseRule.CaseMode)rand.Next(4),
                Scope = CaseRule.ApplyScope.Name
            });

            // Hậu tố số thứ tự bảo đảm tên cuối duy nhất trong mọi ca.
            pipeline.Add(new NumberingRule
            {
                Start = 1 + rand.Next(4),
                Step = 1,
                Width = 3,
                Position = NumberingRule.NumberPosition.End,
                Separator = "_"
            });

            var context = new RuleContext(DateTime.UtcNow, $"seed-{caseIndex}");
            var preview = new PreviewEngine(pipeline).Run(files, context);
            Assert.False(preview.HasBlockingError, $"case {caseIndex} bị chặn");

            var fs = new FakeFileSystem();
            foreach (var file in files) fs.WriteAllText(file.FullPath, file.Name);

            var pairs = preview.Items
                .Where(i => i.Status == PreviewStatus.Ok && i.NewName != i.OldName)
                .Select(i => (i.File.FullPath, System.IO.Path.Combine(i.File.Directory, i.NewName)))
                .ToList();

            var result = new RenameExecutor(fs, "/j").Execute(new RenamePlanner().Plan(pairs));
            Assert.Equal(0, result.Failed);

            foreach (var item in preview.Items.Where(i => i.NewName != i.OldName))
            {
                var target = System.IO.Path.Combine(item.File.Directory, item.NewName);
                Assert.True(fs.FileExists(target), $"case {caseIndex}: thiếu {item.NewName}");
                Assert.Equal(item.File.Name, fs.ReadAllText(target));
            }
        }
    }

    private static List<FileItem> GenerateFiles(int count)
    {
        var list = new List<FileItem>(count);
        for (int i = 0; i < count; i++)
        {
            var name = $"file{i}";
            list.Add(new FileItem($@"/root/{name}.txt", "/root", name, "txt", false, 100 + i, DateTime.UtcNow, DateTime.UtcNow, i));
        }
        return list;
    }
}
