namespace BFR.Core.Tests;

using BFR.Core;
using System.Text.Json;
using Xunit;

public class TokenEngineTests
{
    private static FileItem File(string name = "IMG_001", string ext = "jpg", string dir = @"C:\Anh")
        => new($@"{dir}\{name}.{ext}", dir, name, ext, false, 2048, new DateTime(2024, 5, 1, 10, 30, 0, DateTimeKind.Utc), DateTime.UtcNow, 0);
    private static readonly RuleContext Ctx = new(new DateTime(2024, 6, 1, 8, 0, 0, DateTimeKind.Local), "seed42");

    [Fact]
    public void Name_and_ext_tokens()
    {
        var engine = new TokenEngine();
        var s = new RenameState("IMG_001", "jpg", 0, 0, new List<string>());
        Assert.Equal("IMG_001.jpg", engine.Expand("{name}.{ext}", File(), s, Ctx));
    }

    [Fact]
    public void Numbering_token_with_width()
    {
        var engine = new TokenEngine();
        var s = new RenameState("a", "txt", 6, 6, new List<string>());
        Assert.Equal("007", engine.Expand("{n:3}", File(), s, Ctx));
    }

    [Fact]
    public void Parent_token()
    {
        var engine = new TokenEngine();
        var s = new RenameState("a", "txt", 0, 0, new List<string>());
        Assert.Equal("Anh_a", engine.Expand("{parent}_{name}", File(), s, Ctx));
    }

    [Fact]
    public void Rand_token_is_deterministic()
    {
        var engine = new TokenEngine();
        var s = new RenameState("a", "txt", 0, 0, new List<string>());
        var first = engine.Expand("{rand:6}", File(), s, Ctx);
        var second = engine.Expand("{rand:6}", File(), s, Ctx);
        Assert.Equal(first, second);
        Assert.Equal(6, first.Length);
    }

    [Fact]
    public void Missing_exif_token_is_reported_and_preserved()
    {
        var engine = new TokenEngine();
        var s = new RenameState("a", "txt", 0, 0, new List<string>());
        var errors = new List<string>();
        var result = engine.Expand("{taken:yyyy}", File(), s, Ctx, errors);
        Assert.Equal("{taken:yyyy}", result);
        Assert.Contains(errors, e => e.Contains("Thiếu dữ liệu"));
    }
}

public class SanitizeRuleTests
{
    [Theory]
    [InlineData("Báo cáo tháng 5", "bao-cao-thang-5")]
    [InlineData("Đường Đà Lạt", "duong-da-lat")]
    public void Slugify_removes_diacritics_and_dashes(string input, string expected)
    {
        var rule = new SanitizeRule { Slugify = true, RemoveDiacritics = true, NormalizeNfc = true };
        var state = new RenameState(input, "txt", 0, 0, new List<string>());
        var result = rule.Apply(new FileItem("/a/x.txt", "/a", input, "txt", false, 1, DateTime.UtcNow, DateTime.UtcNow, 0), state, new RuleContext(DateTime.UtcNow, "s"));
        Assert.Equal(expected, result.Name);
    }

    [Fact]
    public void Dd_maps_correctly()
    {
        Assert.Equal("Dd", SanitizeRule.RemoveVietnameseDiacritics("Đđ"));
    }
}

public class CaseRuleTests
{
    [Fact]
    public void Title_case_respects_exceptions()
    {
        var rule = new CaseRule { Mode = CaseRule.CaseMode.Title };
        rule.Exceptions.Add("PDF");
        var state = new RenameState("my PDF file", "txt", 0, 0, new List<string>());
        var result = rule.Apply(new FileItem("/a/x.txt", "/a", "x", "txt", false, 1, DateTime.UtcNow, DateTime.UtcNow, 0), state, new RuleContext(DateTime.UtcNow, "s"));
        Assert.Equal("My PDF File", result.Name);
    }

    [Fact]
    public void Lower_scope_both()
    {
        var rule = new CaseRule { Mode = CaseRule.CaseMode.Lower, Scope = CaseRule.ApplyScope.Both };
        var state = new RenameState("MiXeD", "JPG", 0, 0, new List<string>());
        var result = rule.Apply(new FileItem("/a/x.JPG", "/a", "x", "JPG", false, 1, DateTime.UtcNow, DateTime.UtcNow, 0), state, new RuleContext(DateTime.UtcNow, "s"));
        Assert.Equal("mixed", result.Name);
        Assert.Equal("jpg", result.Extension);
    }
}

public class DateRuleTests
{
    [Fact]
    public void Falls_back_to_created_when_no_exif()
    {
        var rule = new DateRule { Source = DateRule.DateSource.Taken, Format = "yyyy-MM-dd", Position = DateRule.DatePosition.Start, Separator = "_" };
        var file = new FileItem("/a/x.jpg", "/a", "x", "jpg", false, 1, new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Local), DateTime.UtcNow, 0);
        var state = new RenameState("x", "jpg", 0, 0, new List<string>());
        var result = rule.Apply(file, state, new RuleContext(DateTime.UtcNow, "s"));
        Assert.StartsWith("2024-05-01_", result.Name);
        Assert.Contains(result.Trace, t => t.Contains("dự phòng"));
    }

    [Fact]
    public void Uses_exif_when_available()
    {
        var rule = new DateRule { Source = DateRule.DateSource.Taken, Format = "yyyy-MM-dd", Position = DateRule.DatePosition.ReplaceAll };
        var ctx = new RuleContext(DateTime.UtcNow, "s", new Dictionary<string, string> { ["taken"] = "2023-01-15T09:00:00" });
        var file = new FileItem("/a/x.jpg", "/a", "x", "jpg", false, 1, DateTime.UtcNow, DateTime.UtcNow, 0);
        var state = new RenameState("x", "jpg", 0, 0, new List<string>());
        var result = rule.Apply(file, state, ctx);
        Assert.Equal("2023-01-15", result.Name);
    }
}

public class ExtensionRuleTests
{
    [Fact]
    public void Change_blocked_without_flag()
    {
        var rule = new ExtensionRule { Mode = ExtensionRule.ExtensionMode.Set, NewExtension = "jpg", AllowExtensionChange = false };
        var state = new RenameState("x", "jpeg", 0, 0, new List<string>());
        var result = rule.Apply(new FileItem("/a/x.jpeg", "/a", "x", "jpeg", false, 1, DateTime.UtcNow, DateTime.UtcNow, 0), state, new RuleContext(DateTime.UtcNow, "s"));
        Assert.Equal("jpeg", result.Extension);
    }

    [Fact]
    public void Change_allowed_with_flag()
    {
        var rule = new ExtensionRule { Mode = ExtensionRule.ExtensionMode.Set, NewExtension = ".jpg", AllowExtensionChange = true };
        var state = new RenameState("x", "jpeg", 0, 0, new List<string>());
        var result = rule.Apply(new FileItem("/a/x.jpeg", "/a", "x", "jpeg", false, 1, DateTime.UtcNow, DateTime.UtcNow, 0), state, new RuleContext(DateTime.UtcNow, "s"));
        Assert.Equal("jpg", result.Extension);
    }
}

public class InsertRuleTests
{
    [Fact]
    public void Insert_at_index()
    {
        var rule = new InsertRule { Text = "-", Position = InsertRule.InsertPosition.AtIndex, Index = 3 };
        var state = new RenameState("abcdef", "txt", 0, 0, new List<string>());
        var result = rule.Apply(new FileItem("/a/x.txt", "/a", "x", "txt", false, 1, DateTime.UtcNow, DateTime.UtcNow, 0), state, new RuleContext(DateTime.UtcNow, "s"));
        Assert.Equal("abc-def", result.Name);
    }
}

public class PreviewEngineTests
{
    private static FileItem F(string name, int order = 0) => new($"/root/{name}", "/root", Path.GetFileNameWithoutExtension(name), Path.GetExtension(name).TrimStart('.'), false, 1, DateTime.UtcNow, DateTime.UtcNow, order);

    [Fact]
    public void Unchanged_when_no_rule_changes_name()
    {
        var pipeline = new RulePipeline();
        pipeline.Add(new FindReplaceRule { Pattern = "zzz", Replacement = "yyy", Scope = FindReplaceRule.ApplyScope.Name });
        var engine = new PreviewEngine(pipeline);
        var result = engine.Run(new[] { F("a.txt") }, new RuleContext(DateTime.UtcNow, "s"));
        Assert.Equal(PreviewStatus.Unchanged, result.Items[0].Status);
        Assert.False(result.HasBlockingError);
    }

    [Fact]
    public void Conflict_when_duplicate_in_batch()
    {
        var pipeline = new RulePipeline();
        pipeline.Add(new TemplateRule { Template = "same" });
        var engine = new PreviewEngine(pipeline);
        var result = engine.Run(new[] { F("a.txt"), F("b.txt") }, new RuleContext(DateTime.UtcNow, "s"));
        Assert.All(result.Items, i => Assert.Equal(PreviewStatus.Conflict, i.Status));
        Assert.True(result.HasBlockingError);
    }

    [Fact]
    public void Invalid_name_blocked()
    {
        var pipeline = new RulePipeline();
        pipeline.Add(new TemplateRule { Template = "CON.txt" });
        var engine = new PreviewEngine(pipeline);
        var result = engine.Run(new[] { F("a.txt") }, new RuleContext(DateTime.UtcNow, "s"));
        Assert.Equal(PreviewStatus.InvalidName, result.Items[0].Status);
        Assert.True(result.HasBlockingError);
    }
}

public class PresetAndFactoryTests
{
    [Fact]
    public void Preset_store_roundtrips()
    {
        var fs = new FakeFileSystem();
        var store = new PresetStore(fs, "/presets");
        var preset = new Preset { Name = "Test", Description = "d", Rules = { new RuleDefinition { Type = "Trim", Params = { ["trimEnds"] = JsonSerializer.SerializeToElement(true) } } } };
        store.Save(preset);
        var loaded = store.Load("Test");
        Assert.NotNull(loaded);
        Assert.Equal("Test", loaded!.Name);
        Assert.Single(loaded.Rules);
    }

    [Fact]
    public void All_builtin_presets_compile()
    {
        var compiler = new PresetCompiler();
        Assert.NotEmpty(BuiltInPresets.All);
        foreach (var preset in BuiltInPresets.All)
        {
            var pipeline = compiler.Compile(preset);
            Assert.True(pipeline.Rules.Count > 0, preset.Name);
        }
    }
}

public class UndoAndRecoveryTests
{
    [Fact]
    public void Undo_restores_original_state()
    {
        var fs = new FakeFileSystem();
        fs.WriteAllText("/root/a.txt", "A");
        var steps = new RenamePlanner().Plan(new[] { ("/root/a.txt", "/root/b.txt") });
        new RenameExecutor(fs, "/journal").Execute(steps);

        Assert.True(fs.FileExists("/root/b.txt"));

        var undo = new UndoService(fs).UndoLast("/journal");
        Assert.Equal(1, undo.Undone);
        Assert.True(fs.FileExists("/root/a.txt"));
        Assert.False(fs.FileExists("/root/b.txt"));
        Assert.Equal("A", fs.ReadAllText("/root/a.txt"));
    }

    [Fact]
    public void Undo_reports_partial_when_target_moved()
    {
        var fs = new FakeFileSystem();
        fs.WriteAllText("/root/a.txt", "A");
        var steps = new RenamePlanner().Plan(new[] { ("/root/a.txt", "/root/b.txt") });
        new RenameExecutor(fs, "/journal").Execute(steps);

        // Người dùng tự đổi tên tệp đích
        fs.Move("/root/b.txt", "/root/c.txt", false);

        var undo = new UndoService(fs).UndoLast("/journal");
        Assert.Equal(0, undo.Undone);
        Assert.Equal(1, undo.Failed);
    }

    [Fact]
    public void Recovery_finds_incomplete_journal_and_completes()
    {
        var fs = new FakeFileSystem();
        fs.WriteAllText("/root/a.txt", "A");
        // Ghi journal dở dang thủ công (không có footer)
        fs.EnsureDirectory("/journal");
        fs.WriteAllText("/journal/j.jsonl",
            "{\"type\":\"header\",\"id\":\"x\",\"totalSteps\":1}\n" +
            "{\"type\":\"step\",\"seq\":0,\"from\":\"/root/a.txt\",\"to\":\"/root/b.txt\",\"kind\":\"Direct\",\"status\":\"pending\"}\n");

        var recovery = new RecoveryService(fs);
        var candidates = recovery.FindIncomplete("/journal");
        Assert.Single(candidates);

        var result = recovery.Complete(candidates[0].JournalPath);
        Assert.True(result.Success);
        Assert.True(fs.FileExists("/root/b.txt"));
    }
}

public class ScannerAndSortTests
{
    [Fact]
    public void Natural_sort_orders_numbers_correctly()
    {
        var cmp = NaturalSortComparer.Instance;
        Assert.True(cmp.Compare("Bai 2.pdf", "Bai 10.pdf") < 0);
    }

    [Fact]
    public void Scanner_reads_files_from_temp_directory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "BFR_SCAN_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "b.txt"), "1");
            File.WriteAllText(Path.Combine(dir, "a.txt"), "2");
            var scanner = new FileScanner();
            var items = scanner.Scan(new[] { dir }, new ScanOptions());
            Assert.Equal(2, items.Count);
            Assert.Equal("a", items[0].Name); // natural sort
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }
}

public class QaTests
{
    [Fact]
    public void QA02_natural_sort_numbering()
    {
        var files = new List<FileItem>
        {
            new("/r/Bai 1.pdf", "/r", "Bai 1", "pdf", false, 1, DateTime.UtcNow, DateTime.UtcNow, 0),
            new("/r/Bai 2.pdf", "/r", "Bai 2", "pdf", false, 1, DateTime.UtcNow, DateTime.UtcNow, 1),
            new("/r/Bai 10.pdf", "/r", "Bai 10", "pdf", false, 1, DateTime.UtcNow, DateTime.UtcNow, 2)
        };
        files.Sort((a, b) => NaturalSortComparer.Instance.Compare(a.Name, b.Name));
        var ordered = files.Select((f, i) => f with { LoadOrder = i, }).ToList();

        var pipeline = new RulePipeline();
        pipeline.Add(new FindReplaceRule { Pattern = "Bai ", Replacement = "", Scope = FindReplaceRule.ApplyScope.Name });
        pipeline.Add(new NumberingRule { Start = 1, Step = 1, Width = 3, Position = NumberingRule.NumberPosition.ReplaceAll });
        var ctx = new RuleContext(DateTime.UtcNow, "s");
        var result = pipeline.Run(ordered, ctx);
        Assert.Equal("001.pdf", result.Items[0].NewName);
        Assert.Equal("002.pdf", result.Items[1].NewName);
        Assert.Equal("003.pdf", result.Items[2].NewName);
    }

    [Fact]
    public void QA08_invalid_regex_does_not_crash()
    {
        var pipeline = new RulePipeline();
        pipeline.Add(new FindReplaceRule { Pattern = "(", IsRegex = true, Scope = FindReplaceRule.ApplyScope.Name, Replacement = "x" });
        var file = new FileItem("/r/a.txt", "/r", "a", "txt", false, 1, DateTime.UtcNow, DateTime.UtcNow, 0);
        var result = pipeline.Run(new[] { file }, new RuleContext(DateTime.UtcNow, "s"));
        Assert.Equal("a.txt", result.Items[0].NewName);
    }
}
