namespace BFR.Core.Tests;

using BFR.Core;
using Xunit;

public class NameValidatorTests
{
    [Fact]
    public void Valid_name_returns_none()
    {
        var v = new NameValidator();
        Assert.Equal(NameIssue.None, v.Validate("document.txt"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_name_rejected(string name)
    {
        var v = new NameValidator();
        Assert.Equal(NameIssue.Empty, v.Validate(name));
    }

    [Theory]
    [InlineData("CON.txt")]
    [InlineData("NUL")]
    [InlineData("LPT1.log")]
    public void Reserved_names_rejected(string name)
    {
        var v = new NameValidator();
        Assert.Equal(NameIssue.ReservedName, v.Validate(name));
    }

    [Theory]
    [InlineData("a<b.txt")]
    [InlineData("a|b.txt")]
    public void Invalid_chars_rejected(string name)
    {
        var v = new NameValidator();
        Assert.Equal(NameIssue.InvalidChar, v.Validate(name));
    }
}

public class FindReplaceTests
{
    [Fact]
    public void Plain_replace_changes_name()
    {
        var rule = new FindReplaceRule { Pattern = "IMG", Replacement = "Photo", Scope = FindReplaceRule.ApplyScope.Name };
        var file = new FileItem(@"C:\a\IMG_001.jpg", @"C:\a", "IMG_001", "jpg", false, 100, DateTime.UtcNow, DateTime.UtcNow, 0);
        var state = new RenameState("IMG_001", "jpg", 0, 0, new List<string>());
        var ctx = new RuleContext(DateTime.UtcNow, "seed");
        var result = rule.Apply(file, state, ctx);
        Assert.Equal("Photo_001", result.Name);
        Assert.Contains(result.Trace, t => t.Contains("FindReplace"));
    }

    [Fact]
    public void Regex_with_group_works()
    {
        var rule = new FindReplaceRule
        {
            Pattern = @"^(\d+)_(.+)$",
            Replacement = "$2_$1",
            IsRegex = true,
            Scope = FindReplaceRule.ApplyScope.Name
        };
        var file = new FileItem(@"C:\a\20240501_server.log", @"C:\a", "20240501_server", "log", false, 100, DateTime.UtcNow, DateTime.UtcNow, 0);
        var state = new RenameState("20240501_server", "log", 0, 0, new List<string>());
        var ctx = new RuleContext(DateTime.UtcNow, "seed");
        var result = rule.Apply(file, state, ctx);
        Assert.Equal("server_20240501", result.Name);
        Assert.Equal("log", result.Extension);
    }

    [Fact]
    public void Invalid_regex_does_not_crash()
    {
        var rule = new FindReplaceRule { Pattern = "[", IsRegex = true, Scope = FindReplaceRule.ApplyScope.Name };
        var file = new FileItem(@"C:\a\a.txt", @"C:\a", "a", "txt", false, 100, DateTime.UtcNow, DateTime.UtcNow, 0);
        var state = new RenameState("a", "txt", 0, 0, new List<string>());
        var ctx = new RuleContext(DateTime.UtcNow, "seed");
        var result = rule.Apply(file, state, ctx);
        Assert.Equal("a", result.Name);
    }
}

public class NumberingTests
{
    [Fact]
    public void Decimal_numbering_pads_zero()
    {
        var rule = new NumberingRule { Start = 1, Step = 1, Width = 3, Position = NumberingRule.NumberPosition.Start, Separator = "_" };
        var file = new FileItem(@"C:\a\bai.txt", @"C:\a", "bai", "txt", false, 100, DateTime.UtcNow, DateTime.UtcNow, 0);
        var state = new RenameState("bai", "txt", 0, 0, new List<string>());
        var ctx = new RuleContext(DateTime.UtcNow, "seed");
        var result = rule.Apply(file, state, ctx);
        Assert.Equal("001_bai", result.Name);
    }
}

public class TrimTests
{
    [Fact]
    public void Trim_removes_chars_and_collapses_spaces()
    {
        var rule = new TrimRule
        {
            RemoveChars = "()",
            CollapseSpaces = true,
            TrimEnds = true,
            RemoveTrailingDotSpace = true
        };
        var file = new FileItem(@"C:\a\  Bai  (1)  .txt", @"C:\a", "  Bai  (1)  ", "txt", false, 100, DateTime.UtcNow, DateTime.UtcNow, 0);
        var state = new RenameState("  Bai  (1)  ", "txt", 0, 0, new List<string>());
        var ctx = new RuleContext(DateTime.UtcNow, "seed");
        var result = rule.Apply(file, state, ctx);
        Assert.Equal("Bai 1", result.Name);
    }
}

public class ConflictDetectorTests
{
    [Fact]
    public void Duplicate_in_batch_detected()
    {
        var det = new ConflictDetector();
        var items = new List<PreviewItem>
        {
            new(new FileItem(@"C:\a\a.txt", @"C:\a", "a", "txt", false, 1, DateTime.UtcNow, DateTime.UtcNow, 0), "a.txt", "x.txt", PreviewStatus.Ok, new List<string>()),
            new(new FileItem(@"C:\a\b.txt", @"C:\a", "b", "txt", false, 1, DateTime.UtcNow, DateTime.UtcNow, 1), "b.txt", "X.TXT", PreviewStatus.Ok, new List<string>())
        };
        var res = det.Detect(items, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        Assert.All(res, r => Assert.NotNull(r));
        Assert.True(res[0]!.Type == ConflictType.DuplicateInBatch);
        Assert.True(res[1]!.Type == ConflictType.DuplicateInBatch);
    }
}

public class RenamePlannerTests
{
    [Fact]
    public void Swap_cycle_uses_temp_name()
    {
        var planner = new RenamePlanner();
        var pairs = new List<(string From, string To)> { ("a.txt", "b.txt"), ("b.txt", "a.txt") };
        var steps = planner.Plan(pairs);
        // Bước đầu phải là ToTemp (vì tạo vòng)
        Assert.Equal(StepKind.ToTemp, steps[0].Kind);
        // Không còn bước Direct nào dẫn từ a.txt về b.txt ngay trước khi xử lý temp
        Assert.Contains(steps, s => s.Kind == StepKind.FromTemp);
        Assert.Equal(3, steps.Count);
    }

    [Fact]
    public void Chain_uses_reverse_topo()
    {
        var planner = new RenamePlanner();
        var pairs = new List<(string From, string To)> { ("a.txt", "b.txt"), ("b.txt", "c.txt") };
        var steps = planner.Plan(pairs);
        // b->c trước (c là đích không bị chặn), rồi a->b
        Assert.Equal(StepKind.Direct, steps[0].Kind);
        Assert.Equal("b.txt", steps[0].From);
        Assert.Equal("c.txt", steps[0].To);
        Assert.Equal(StepKind.Direct, steps[1].Kind);
        Assert.Equal("a.txt", steps[1].From);
        Assert.Equal("b.txt", steps[1].To);
    }
}

public class FakeFileSystemTests
{
    [Fact]
    public void Move_works_in_memory()
    {
        var fs = new FakeFileSystem();
        fs.WriteAllText("/root/a.txt", "hello");
        fs.EnsureDirectory("/root/sub");
        fs.Move("/root/a.txt", "/root/sub/a.txt", false);
        Assert.True(fs.FileExists("/root/sub/a.txt"));
        Assert.False(fs.FileExists("/root/a.txt"));
        Assert.Equal("hello", fs.ReadAllText("/root/sub/a.txt"));
    }

    [Fact]
    public void Move_throws_when_throw_on_move()
    {
        var fs = new FakeFileSystem { ThrowOnMove = true };
        fs.WriteAllText("/a.txt", "x");
        Assert.Throws<IOException>(() => fs.Move("/a.txt", "/b.txt", false));
    }
}
