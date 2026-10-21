namespace BFR.Presentation.Tests;

using BFR.Presentation;
using BFR.Core;
using BFR.Core.Licensing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

/// <summary>
/// Test cho tầng ViewModel của giao diện v2.0 (docs/Plan/BFR_UIUX_REBUILD_PLAN.md, mục 7).
/// Không đụng BFR.Core nên kết quả INV-1..INV-6 không đổi.
/// </summary>
public class ViewModelTests : IDisposable
{
    private readonly string _temp = Path.Combine(Path.GetTempPath(), "bfr-vm-" + Guid.NewGuid().ToString("N"));

    public ViewModelTests()
    {
        Directory.CreateDirectory(_temp);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_temp)) Directory.Delete(_temp, recursive: true); } catch (IOException) { }
    }

    private MainViewModel CreateViewModel(LicenseTier tier = LicenseTier.Free)
    {
        var fs = new RealFileSystem();
        var license = new LicenseService(new LicenseStore(fs, Path.Combine(_temp, "license.json")), trialEnabled: false);
        if (tier == LicenseTier.Pro) license.RecordPurchase();
        var coordinator = new LicenseCoordinator(license);
        return new MainViewModel(coordinator, null, _temp);
    }

    // ----------------------------------------------------------------- Sắp xếp

    [Fact]
    public void SortItems_Natural_OrdersBai2BeforeBai10()
    {
        var files = new List<FileItem>
        {
            Item("Bai 10.txt", 10),
            Item("Bai 2.txt", 2),
            Item("Bai 1.txt", 1)
        };

        var sorted = MainViewModel.SortItems(files, SortBy.NameNatural);

        Assert.Equal(new[] { "Bai 1", "Bai 2", "Bai 10" }, sorted.Select(f => f.Name));
    }

    [Fact]
    public void SortItems_BySize_Descending()
    {
        var files = new List<FileItem>
        {
            Item("a.txt", 10),
            Item("b.txt", 300),
            Item("c.txt", 100)
        };

        var sorted = MainViewModel.SortItems(files, SortBy.Size);

        Assert.Equal(new[] { "b", "c", "a" }, sorted.Select(f => f.Name));
    }

    [Fact]
    public void SortItems_ByLoadOrder_KeepsInputOrder()
    {
        var files = new List<FileItem>
        {
            Item("z.txt", 1, loadOrder: 2),
            Item("a.txt", 1, loadOrder: 0),
            Item("m.txt", 1, loadOrder: 1)
        };

        var sorted = MainViewModel.SortItems(files, SortBy.LoadOrder);

        Assert.Equal(new[] { "a", "m", "z" }, sorted.Select(f => f.Name));
    }

    // ----------------------------------------------------------------- Diff

    [Fact]
    public void Diff_MarksOnlyChangedMiddle()
    {
        var (oldSegments, newSegments) = PreviewRowViewModel.Diff("Bai 01.txt", "Bai 02.txt");

        // Tiền tố "Bai " + phần đổi "01" + hậu tố ".txt"
        Assert.Equal(3, oldSegments.Count);
        Assert.False(oldSegments[0].Highlight);
        Assert.True(oldSegments[1].Highlight);
        Assert.Equal("1", oldSegments[1].Text);
        Assert.Equal("2", newSegments[1].Text);
        Assert.False(oldSegments[2].Highlight);
    }

    [Fact]
    public void Diff_IdenticalNames_HasNoHighlight()
    {
        var (oldSegments, newSegments) = PreviewRowViewModel.Diff("abc.txt", "abc.txt");

        Assert.Single(oldSegments);
        Assert.False(oldSegments[0].Highlight);
        Assert.Single(newSegments);
    }

    // ----------------------------------------------------------------- Rule card

    [Fact]
    public void RuleCard_ParameterEditsAreWrittenBackToDefinition()
    {
        var definition = new RuleDefinition
        {
            Type = "Numbering",
            Params = new Dictionary<string, System.Text.Json.JsonElement>
            {
                ["start"] = System.Text.Json.JsonSerializer.SerializeToElement(1),
                ["width"] = System.Text.Json.JsonSerializer.SerializeToElement(3)
            }
        };

        var card = new RuleCardViewModel(definition);
        var start = card.Parameters.Single(p => p.Key == "start");
        var width = card.Parameters.Single(p => p.Key == "width");

        start.TextValue = "5";
        width.TextValue = "4";

        Assert.Equal(5, definition.Params["start"].GetInt32());
        Assert.Equal(4, definition.Params["width"].GetInt32());
    }

    [Fact]
    public void RuleCard_ToggleEnabled_UpdatesDefinition()
    {
        var definition = new RuleDefinition { Type = "Trim", Enabled = true };
        var card = new RuleCardViewModel(definition);

        card.IsEnabled = false;

        Assert.False(definition.Enabled);
    }

    [Fact]
    public void RuleCard_Summary_DescribesRule()
    {
        var definition = new RuleDefinition
        {
            Type = "FindReplace",
            Params = new Dictionary<string, System.Text.Json.JsonElement>
            {
                ["pattern"] = System.Text.Json.JsonSerializer.SerializeToElement("(1)"),
                ["replacement"] = System.Text.Json.JsonSerializer.SerializeToElement("")
            }
        };

        var card = new RuleCardViewModel(definition);

        Assert.Contains("(1)", card.Summary);
    }

    // ----------------------------------------------------------------- ViewModel

    [Fact]
    public async Task ViewModel_LoadsFiles_ComputesCounts_AndShowsDiff()
    {
        var dir = Path.Combine(_temp, "data");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "IMG 1.txt"), "x");
        File.WriteAllText(Path.Combine(dir, "IMG 2.txt"), "x");

        var vm = CreateViewModel();
        vm.LoadPreset(BuiltInPresets.All.First(p => p.Name.Contains("Đánh số")));
        vm.AddPaths(new[] { dir });
        await vm.RebuildAsync();

        Assert.Equal(2, vm.TotalCount);
        Assert.Equal(2, vm.WillChangeCount);
        Assert.Equal(0, vm.ErrorCount);
        Assert.True(vm.CanApply);
        // Nhãn nút lấy từ resource (không có Application trong test) -> kiểm tra số lượng.\n        Assert.Contains("2", vm.ApplyButtonText);
    }

    [Fact]
    public async Task ViewModel_ExcludedFile_IsCountedButNotApplied()
    {
        var dir = Path.Combine(_temp, "data");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "IMG 1.txt"), "x");
        File.WriteAllText(Path.Combine(dir, "IMG 2.txt"), "x");

        var vm = CreateViewModel();
        vm.LoadPreset(BuiltInPresets.All.First(p => p.Name.Contains("Đánh số")));
        vm.AddPaths(new[] { dir });
        await vm.RebuildAsync();

        vm.Rows[0].IsSelected = false;

        Assert.Equal(1, vm.WillChangeCount);
    }

    [Fact]
    public async Task ViewModel_ApplySummary_ReportsFoldersAndExtensions()
    {
        var dir = Path.Combine(_temp, "data");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "a.txt"), "x");

        var vm = CreateViewModel();
        vm.LoadPreset(BuiltInPresets.All.First(p => p.Name.Contains("Đánh số")));
        vm.AddPaths(new[] { dir });
        await vm.RebuildAsync();

        var summary = vm.BuildApplySummary();

        Assert.Equal(1, summary.FileCount);
        Assert.Equal(1, summary.FolderCount);
        Assert.NotEmpty(summary.Samples);
    }

    [Fact]
    public void ViewModel_FreeTier_BlocksAddingFourthRule()
    {
        var vm = CreateViewModel();
        vm.LoadPreset(new Preset { Name = "p" });

        Assert.True(vm.AddRule("Trim"));
        Assert.True(vm.AddRule("Case"));
        Assert.True(vm.AddRule("Sanitize"));
        Assert.False(vm.AddRule("Insert"));   // vượt giới hạn Free (3 quy tắc)
        Assert.Equal(3, vm.Rules.Count);
    }

    [Fact]
    public void ViewModel_ProTier_AllowsUnlimitedRules()
    {
        var vm = CreateViewModel(LicenseTier.Pro);
        vm.LoadPreset(new Preset { Name = "p" });

        for (int i = 0; i < 8; i++) Assert.True(vm.AddRule("Trim"));
        Assert.Equal(8, vm.Rules.Count);
    }

    [Fact]
    public void ViewModel_MoveRule_ReordersChain()
    {
        var vm = CreateViewModel();
        vm.LoadPreset(new Preset
        {
            Name = "p",
            Rules = new List<RuleDefinition>
            {
                new() { Type = "Trim" },
                new() { Type = "Case" }
            }
        });

        vm.MoveRule(vm.Rules[1], -1);

        Assert.Equal("Case", vm.Rules[0].Type);
        Assert.Equal("Trim", vm.Rules[1].Type);
    }

    private static FileItem Item(string name, long size, int loadOrder = 0) => new(
        FullPath: Path.Combine("C:\\data", name),
        Directory: "C:\\data",
        Name: Path.GetFileNameWithoutExtension(name),
        Extension: Path.GetExtension(name).TrimStart('.'),
        IsDirectory: false,
        Size: size,
        CreatedUtc: DateTime.UtcNow,
        ModifiedUtc: DateTime.UtcNow,
        LoadOrder: loadOrder);
}
