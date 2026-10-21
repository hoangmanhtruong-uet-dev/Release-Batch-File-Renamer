namespace BFR.Core.Tests;

using BFR.Core;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Xunit;

/// <summary>
/// Các ca QA của Giai đoạn 1: QA-01, 04, 05, 06, 07, 09, 11, 13 cùng kiểm thử
/// HistoryStore và RuleFactory.
/// </summary>
public class Phase1QaTests
{
    private static FileItem MakeFile(string name, int order = 0, string dir = "/r")
        => new($"{dir}/{name}", dir, Path.GetFileNameWithoutExtension(name), Path.GetExtension(name).TrimStart('.'), false, 10, DateTime.UtcNow, DateTime.UtcNow, order);

    private static FakeFileSystem Apply(
        IReadOnlyList<(string From, string To)> pairs,
        params (string Path, string Content)[] files)
    {
        var fs = new FakeFileSystem();
        foreach (var (path, content) in files) fs.WriteAllText(path, content);

        var steps = new RenamePlanner().Plan(pairs);
        var result = new RenameExecutor(fs, "/j").Execute(steps);
        Assert.Equal(0, result.Failed);
        Assert.DoesNotContain(fs.GetFiles("/", "*", true), p => p.Contains(".bfr-"));
        return fs;
    }

    [Fact]
    public void QA01_preview_matches_apply_for_40_files()
    {
        var files = Enumerable.Range(0, 40)
            .Select(i => MakeFile($"file{i}.txt", i))
            .ToList();

        var pipeline = new RulePipeline();
        pipeline.Add(new FindReplaceRule
        {
            Pattern = "file",
            Replacement = "anh",
            Scope = FindReplaceRule.ApplyScope.Name
        });
        pipeline.Add(new NumberingRule
        {
            Start = 1,
            Step = 1,
            Width = 3,
            Position = NumberingRule.NumberPosition.End,
            Separator = "_"
        });

        var preview = new PreviewEngine(pipeline).Run(files, new RuleContext(DateTime.UtcNow, "seed"));
        Assert.False(preview.HasBlockingError);

        var fs = new FakeFileSystem();
        foreach (var file in files) fs.WriteAllText(file.FullPath, file.Name);

        var pairs = preview.Items
            .Where(i => i.Status == PreviewStatus.Ok && i.NewName != i.OldName)
            .Select(i => (i.File.FullPath, Path.Combine(i.File.Directory, i.NewName)))
            .ToList();

        var result = new RenameExecutor(fs, "/j").Execute(new RenamePlanner().Plan(pairs));
        Assert.Equal(0, result.Failed);

        foreach (var item in preview.Items.Where(i => i.NewName != i.OldName))
        {
            var expected = Path.Combine(item.File.Directory, item.NewName);
            Assert.True(fs.FileExists(expected), expected);
            Assert.Equal(item.File.Name, fs.ReadAllText(expected)); // nội dung không đổi
        }
    }

    [Fact]
    public void QA04_swap_two_files()
    {
        var fs = Apply(
            new[] { ("/r/a.txt", "/r/b.txt"), ("/r/b.txt", "/r/a.txt") },
            ("/r/a.txt", "A"), ("/r/b.txt", "B"));

        Assert.Equal("B", fs.ReadAllText("/r/a.txt"));
        Assert.Equal("A", fs.ReadAllText("/r/b.txt"));
    }

    [Fact]
    public void QA04_chain_three_files()
    {
        var fs = Apply(
            new[] { ("/r/a.txt", "/r/b.txt"), ("/r/b.txt", "/r/c.txt"), ("/r/c.txt", "/r/d.txt") },
            ("/r/a.txt", "A"), ("/r/b.txt", "B"), ("/r/c.txt", "C"));

        Assert.Equal("A", fs.ReadAllText("/r/b.txt"));
        Assert.Equal("B", fs.ReadAllText("/r/c.txt"));
        Assert.Equal("C", fs.ReadAllText("/r/d.txt"));
    }

    [Fact]
    public void QA04_cycle_three_files()
    {
        var fs = Apply(
            new[] { ("/r/a.txt", "/r/b.txt"), ("/r/b.txt", "/r/c.txt"), ("/r/c.txt", "/r/a.txt") },
            ("/r/a.txt", "A"), ("/r/b.txt", "B"), ("/r/c.txt", "C"));

        Assert.Equal("C", fs.ReadAllText("/r/a.txt"));
        Assert.Equal("A", fs.ReadAllText("/r/b.txt"));
        Assert.Equal("B", fs.ReadAllText("/r/c.txt"));
    }

    [Fact]
    public void QA05_complete_and_rollback_after_crash_at_every_step()
    {
        const int count = 200;
        var pairs = Enumerable.Range(0, count)
            .Select(i => ($"/r/f{i}.txt", $"/r/g{i}.txt"))
            .ToList();
        var steps = new RenamePlanner().Plan(pairs);
        Assert.Equal(count, steps.Count);

        for (int crashAt = 0; crashAt <= steps.Count; crashAt++)
        {
            var fs = new FakeFileSystem();
            for (int i = 0; i < count; i++) fs.WriteAllText($"/r/f{i}.txt", $"content-{i}");
            fs.EnsureDirectory("/j");

            // Mô phỏng đã chạy xong crashAt bước đầu.
            for (int j = 0; j < crashAt; j++) fs.Move(steps[j].From, steps[j].To, overwrite: false);

            var journal = new StringBuilder();
            void Line(JournalEntry entry) => journal.AppendLine(JsonSerializer.Serialize(entry));

            Line(new JournalEntry { Type = "header", Id = "test", TotalSteps = steps.Count });
            for (int j = 0; j < crashAt; j++)
            {
                Line(new JournalEntry
                {
                    Type = "step", Seq = j, From = steps[j].From, To = steps[j].To,
                    Kind = steps[j].Kind.ToString(), Status = "done"
                });
            }
            if (crashAt < steps.Count)
            {
                Line(new JournalEntry
                {
                    Type = "step", Seq = crashAt, From = steps[crashAt].From, To = steps[crashAt].To,
                    Kind = steps[crashAt].Kind.ToString(), Status = "pending"
                });
            }
            fs.WriteAllText("/j/j.jsonl", journal.ToString());

            var recovery = new RecoveryService(fs);
            var complete = recovery.Complete("/j/j.jsonl");
            Assert.True(complete.Success, $"Complete thất bại tại crashAt={crashAt}");

            // Bước pending vừa rồi phải được hoàn tất.
            if (crashAt < steps.Count)
                Assert.True(fs.FileExists(steps[crashAt].To), $"Thiếu đích tại crashAt={crashAt}");

            var rollback = recovery.Rollback("/j/j.jsonl");
            Assert.True(rollback.Success, $"Rollback thất bại tại crashAt={crashAt}");

            for (int i = 0; i < count; i++)
            {
                Assert.True(fs.FileExists($"/r/f{i}.txt"), $"crashAt={crashAt}: mất f{i}.txt");
                Assert.False(fs.FileExists($"/r/g{i}.txt"), $"crashAt={crashAt}: còn sót g{i}.txt");
            }
        }
    }

    [Fact]
    public void QA06_partial_undo_reports_reason_for_changed_file()
    {
        var fs = new FakeFileSystem();
        for (int i = 0; i < 3; i++) fs.WriteAllText($"/r/f{i}.txt", $"content-{i}");

        var pairs = Enumerable.Range(0, 3)
            .Select(i => ($"/r/f{i}.txt", $"/r/g{i}.txt"))
            .ToList();
        new RenameExecutor(fs, "/j").Execute(new RenamePlanner().Plan(pairs));

        // Người dùng tự đổi tên một tệp đích trước khi hoàn tác.
        fs.Move("/r/g1.txt", "/r/h1.txt", overwrite: false);

        var undo = new UndoService(fs).UndoLast("/j");
        Assert.Equal(2, undo.Undone);
        Assert.Equal(1, undo.Failed);
        Assert.Single(undo.Errors);

        Assert.True(fs.FileExists("/r/f0.txt"));
        Assert.True(fs.FileExists("/r/f2.txt"));
        Assert.True(fs.FileExists("/r/h1.txt"));
        Assert.False(fs.FileExists("/r/g0.txt"));
        Assert.False(fs.FileExists("/r/g2.txt"));
    }

    [Fact]
    public void FindReplace_plain_text_keeps_dollar_and_braces_literal()
    {
        var rule = new FindReplaceRule
        {
            Pattern = "price",
            Replacement = "$100 {foo}",
            Scope = FindReplaceRule.ApplyScope.Name
        };
        var state = new RenameState("price", "txt", 0, 0, new List<string>());
        var result = rule.Apply(MakeFile("price.txt"), state, new RuleContext(DateTime.UtcNow, "s"));

        Assert.Equal("$100 {foo}", result.Name);
    }

    [Fact]
    public void FindReplace_expands_tokens_in_replacement()
    {
        var rule = new FindReplaceRule
        {
            Pattern = "IMG",
            Replacement = "{parent}_{n:3}",
            Scope = FindReplaceRule.ApplyScope.Name
        };
        var state = new RenameState("IMG", "jpg", 4, 4, new List<string>());
        var result = rule.Apply(MakeFile("IMG.jpg"), state, new RuleContext(DateTime.UtcNow, "s"));

        Assert.Equal("r_005", result.Name);
    }

    [Fact]
    public void QA08_catastrophic_regex_times_out_without_crashing()
    {
        var rule = new FindReplaceRule
        {
            Pattern = "(a+)+$",
            Replacement = "x",
            IsRegex = true,
            Scope = FindReplaceRule.ApplyScope.Name
        };
        var name = new string('a', 120) + "b";
        var state = new RenameState(name, "txt", 0, 0, new List<string>());
        var result = rule.Apply(MakeFile(name + ".txt"), state, new RuleContext(DateTime.UtcNow, "s"));

        Assert.Equal(name, result.Name);
        Assert.Contains(result.Trace, t => t.Contains("timeout", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void QA07_conflicts_are_classified()
    {
        var detector = new ConflictDetector();
        var outside = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "outside.txt" };

        var duplicate = detector.Detect(new[]
        {
            new PreviewItem(MakeFile("a.txt"), "a.txt", "same.txt", PreviewStatus.Ok, new List<string>()),
            new PreviewItem(MakeFile("b.txt"), "b.txt", "SAME.TXT", PreviewStatus.Ok, new List<string>())
        }, outside);
        Assert.All(duplicate, c => Assert.Equal(ConflictType.DuplicateInBatch, c!.Type));

        var collision = detector.Detect(new[]
        {
            new PreviewItem(MakeFile("a.txt"), "a.txt", "outside.txt", PreviewStatus.Ok, new List<string>())
        }, outside);
        Assert.Equal(ConflictType.CollisionOutsideBatch, collision[0]!.Type);

        var reserved = detector.Detect(new[]
        {
            new PreviewItem(MakeFile("a.txt"), "a.txt", "CON.txt", PreviewStatus.Ok, new List<string>())
        }, outside);
        Assert.Equal(ConflictType.ReservedName, reserved[0]!.Type);

        var trailing = detector.Detect(new[]
        {
            new PreviewItem(MakeFile("a.txt"), "a.txt", "name. ", PreviewStatus.Ok, new List<string>())
        }, outside);
        Assert.Equal(ConflictType.InvalidName, trailing[0]!.Type);
    }

    [Fact]
    public void QA07_same_new_name_in_different_directories_is_not_a_conflict()
    {
        var detector = new ConflictDetector();
        var items = new[]
        {
            new PreviewItem(new FileItem("/a/x.txt", "/a", "x", "txt", false, 1, DateTime.UtcNow, DateTime.UtcNow, 0),
                "x.txt", "same.txt", PreviewStatus.Ok, new List<string>()),
            new PreviewItem(new FileItem("/b/y.txt", "/b", "y", "txt", false, 1, DateTime.UtcNow, DateTime.UtcNow, 1),
                "y.txt", "same.txt", PreviewStatus.Ok, new List<string>())
        };

        var result = detector.Detect(items, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        Assert.All(result, r => Assert.Null(r));
    }

    [Fact]
    public void QA07_outside_batch_index_detects_existing_file_in_same_directory()
    {
        var fs = new FakeFileSystem();
        fs.WriteAllText("/r/existing.txt", "x");
        fs.WriteAllText("/r/a.txt", "a");

        var batch = new[]
        {
            new FileItem("/r/a.txt", "/r", "a", "txt", false, 1, DateTime.UtcNow, DateTime.UtcNow, 0)
        };

        var pipeline = new RulePipeline();
        pipeline.Add(new TemplateRule { Template = "existing" });
        var index = OutsideBatchIndex.Build(batch, fs);
        var preview = new PreviewEngine(pipeline).Run(batch, new RuleContext(DateTime.UtcNow, "s"), null, default, index);

        Assert.Equal(PreviewStatus.Conflict, preview.Items[0].Status);
        Assert.Contains("ngoài lô", preview.Items[0].Detail);
    }

    [Fact]
    public void QA07_outside_batch_index_detects_existing_directory_for_folder_batch()
    {
        var fs = new FakeFileSystem();
        fs.EnsureDirectory("/r/existingDir");
        fs.EnsureDirectory("/r/sub");

        var batch = new[]
        {
            new FileItem("/r/sub", "/r", "sub", string.Empty, true, 0, DateTime.UtcNow, DateTime.UtcNow, 0)
        };

        var pipeline = new RulePipeline();
        pipeline.Add(new TemplateRule { Template = "existingDir" });
        var index = OutsideBatchIndex.Build(batch, fs);
        var preview = new PreviewEngine(pipeline).Run(batch, new RuleContext(DateTime.UtcNow, "s"), null, default, index);

        Assert.Equal(PreviewStatus.Conflict, preview.Items[0].Status);
    }

    [Fact]
    public void QA07_preview_blocks_conflict_and_reserved_name()
    {
        var pipeline = new RulePipeline();
        pipeline.Add(new TemplateRule { Template = "CON.txt" });
        var preview = new PreviewEngine(pipeline).Run(new[] { MakeFile("a.txt") }, new RuleContext(DateTime.UtcNow, "s"));

        Assert.Equal(PreviewStatus.InvalidName, preview.Items[0].Status);
        Assert.True(preview.HasBlockingError);
    }

    [Fact]
    public void QA09_metadata_requirement_is_detected()
    {
        var withTaken = new RulePipeline();
        withTaken.Add(new DateRule { Source = DateRule.DateSource.Taken });
        Assert.True(MetadataRequirement.NeedsMetadata(withTaken));

        var withCreated = new RulePipeline();
        withCreated.Add(new DateRule { Source = DateRule.DateSource.Created });
        Assert.False(MetadataRequirement.NeedsMetadata(withCreated));

        var withToken = new RulePipeline();
        withToken.Add(new TemplateRule { Template = "{taken:yyyy}" });
        Assert.True(MetadataRequirement.NeedsMetadata(withToken));
    }

    [Fact]
    public void QA09_exif_reader_returns_empty_for_non_image()
    {
        var path = Path.Combine(Path.GetTempPath(), "bfr_exif_" + Guid.NewGuid().ToString("N") + ".txt");
        File.WriteAllText(path, "not an image");
        try
        {
            var file = new FileItem(path, Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path),
                "txt", false, 10, DateTime.UtcNow, DateTime.UtcNow, 0);
            var metadata = new ExifMetadataReader().Read(file);
            Assert.Empty(metadata);
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    [Fact]
    public void QA11_locked_file_fails_individually_and_others_continue()
    {
        var fs = new FakeFileSystem();
        for (int i = 0; i < 3; i++) fs.WriteAllText($"/r/f{i}.txt", $"content-{i}");
        fs.FailOnMovePaths.Add("/r/f1.txt"); // giả lập tệp đang bị khóa

        var pairs = Enumerable.Range(0, 3)
            .Select(i => ($"/r/f{i}.txt", $"/r/g{i}.txt"))
            .ToList();
        var result = new RenameExecutor(fs, "/j").Execute(new RenamePlanner().Plan(pairs));

        Assert.Equal(2, result.Succeeded);
        Assert.Equal(1, result.Failed);
        Assert.True(fs.FileExists("/r/g0.txt"));
        Assert.True(fs.FileExists("/r/f1.txt")); // tệp khóa vẫn nguyên
        Assert.True(fs.FileExists("/r/g2.txt"));
    }

    [Fact]
    public void QA13_unicode_and_emoji_are_valid()
    {
        var pipeline = new RulePipeline();
        pipeline.Add(new TemplateRule { Template = "ảnh-😀-{n}" });
        var preview = new PreviewEngine(pipeline).Run(new[] { MakeFile("a.txt") }, new RuleContext(DateTime.UtcNow, "s"));

        Assert.Equal(PreviewStatus.Ok, preview.Items[0].Status);
        Assert.Contains("😀", preview.Items[0].NewName);
    }

    [Fact]
    public void QA13_path_over_260_is_blocked()
    {
        var longDir = @"C:\" + new string('a', 250);
        var file = new FileItem($@"{longDir}\a.txt", longDir, "a", "txt", false, 10, DateTime.UtcNow, DateTime.UtcNow, 0);

        var pipeline = new RulePipeline();
        pipeline.Add(new TemplateRule { Template = "new" });
        var preview = new PreviewEngine(pipeline).Run(new[] { file }, new RuleContext(DateTime.UtcNow, "s"));

        Assert.Equal(PreviewStatus.PathTooLong, preview.Items[0].Status);
        Assert.True(preview.HasBlockingError);
    }

    [Fact]
    public void History_store_orders_newest_first_and_roundtrips()
    {
        var fs = new FakeFileSystem();
        var store = new HistoryStore(fs, "/app/history.json");

        store.Add(new HistoryEntry { Id = "first", StartedUtc = DateTime.UtcNow.AddMinutes(-2), Total = 1, Succeeded = 1 });
        store.Add(new HistoryEntry { Id = "second", StartedUtc = DateTime.UtcNow, Total = 2, Succeeded = 2 });

        var all = store.LoadAll();
        Assert.Equal(2, all.Count);
        Assert.Equal("second", all[0].Id);
        Assert.Equal("first", all[1].Id);
    }

    [Theory]
    [InlineData("FindReplace")]
    [InlineData("Numbering")]
    [InlineData("Trim")]
    [InlineData("Insert")]
    [InlineData("Case")]
    [InlineData("Sanitize")]
    [InlineData("Date")]
    [InlineData("Extension")]
    [InlineData("Template")]
    public void Rule_factory_supports_every_free_rule_type(string type)
    {
        var definition = new RuleDefinition { Type = type };
        var rule = RuleFactory.Create(definition, allowExtensionChange: true);
        Assert.NotNull(rule);
        Assert.True(rule.Enabled);
    }
}
