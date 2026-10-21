namespace BFR.Presentation.Tests;

using BFR.Core;
using BFR.Core.Licensing;
using BFR.Presentation;

using System;
using System.IO;
using System.Linq;

using Xunit;

/// <summary>
/// Test cho việc đổi thứ tự quy tắc (kéo-thả + nút lên/xuống) — mục A2.
/// </summary>
public class RuleOrderTests : IDisposable
{
    private readonly string _temp = Path.Combine(Path.GetTempPath(), "bfr-order-" + Guid.NewGuid().ToString("N"));

    public RuleOrderTests() => Directory.CreateDirectory(_temp);

    public void Dispose()
    {
        try { if (Directory.Exists(_temp)) Directory.Delete(_temp, recursive: true); } catch (IOException) { }
    }

    private MainViewModel CreateViewModel()
    {
        var fs = new RealFileSystem();
        var license = new LicenseService(new LicenseStore(fs, Path.Combine(_temp, "license.json")), trialEnabled: false);
        return new MainViewModel(new LicenseCoordinator(license), null, _temp);
    }

    [Fact]
    public void AddRule_AssignsOrderFlags()
    {
        var vm = CreateViewModel();

        Assert.True(vm.AddRule("Trim"));
        Assert.True(vm.AddRule("Insert"));

        Assert.False(vm.Rules[0].CanMoveUp);
        Assert.True(vm.Rules[0].CanMoveDown);
        Assert.True(vm.Rules[1].CanMoveUp);
        Assert.False(vm.Rules[1].CanMoveDown);
    }

    [Fact]
    public void MoveRule_MovesCardAndPresetTogether()
    {
        var vm = CreateViewModel();
        vm.AddRule("Trim");
        vm.AddRule("Insert");
        vm.AddRule("Case");

        var first = vm.Rules[0];
        vm.MoveRule(first, 1);

        Assert.Equal("Insert", vm.Rules[0].Type);
        Assert.Equal("Trim", vm.Rules[1].Type);

        // Preset phải cùng thứ tự để kết quả đánh số giống nhau.
        var preset = vm.CurrentPreset;
        Assert.Equal(new[] { "Insert", "Trim", "Case" }, preset.Rules.Select(r => r.Type));
    }

    [Fact]
    public void MoveRuleTo_MovesToArbitraryIndex()
    {
        var vm = CreateViewModel();
        vm.AddRule("Trim");
        vm.AddRule("Insert");
        vm.AddRule("Case");

        var last = vm.Rules[2];
        Assert.True(vm.MoveRuleTo(last, 0));

        Assert.Equal("Case", vm.Rules[0].Type);
        Assert.Equal(new[] { "Case", "Trim", "Insert" }, vm.CurrentPreset.Rules.Select(r => r.Type));
    }

    [Fact]
    public void MoveRuleTo_OutOfRange_ReturnsFalse()
    {
        var vm = CreateViewModel();
        vm.AddRule("Trim");

        Assert.False(vm.MoveRuleTo(vm.Rules[0], 5));
        Assert.False(vm.MoveRuleTo(vm.Rules[0], -1));
    }

    [Fact]
    public void RemoveRule_RefreshesOrderFlags()
    {
        var vm = CreateViewModel();
        vm.AddRule("Trim");
        vm.AddRule("Insert");

        vm.RemoveRule(vm.Rules[0]);

        Assert.Single(vm.Rules);
        Assert.False(vm.Rules[0].CanMoveUp);
        Assert.False(vm.Rules[0].CanMoveDown);
    }

    [Fact]
    public void CardCommands_MoveRuleUpAndDown()
    {
        var vm = CreateViewModel();
        vm.AddRule("Trim");
        vm.AddRule("Insert");

        vm.Rules[1].MoveUpCommand.Execute(null);

        Assert.Equal("Insert", vm.Rules[0].Type);

        vm.Rules[0].MoveDownCommand.Execute(null);
        Assert.Equal("Trim", vm.Rules[0].Type);
    }

    [Fact]
    public void OnlyFindReplaceRule_CanOpenRegexTester()
    {
        var vm = CreateViewModel();
        vm.AddRule("Trim");
        vm.AddRule("FindReplace");

        Assert.False(vm.Rules[0].CanOpenRegexTester);
        Assert.True(vm.Rules[1].CanOpenRegexTester);
    }

    [Fact]
    public void ParameterLabels_AreLocalizedThroughResourceKeys()
    {
        var vm = CreateViewModel();
        vm.AddRule("FindReplace");

        var pattern = vm.Rules[0].Parameters.First(p => p.Key == "pattern");

        // Bản test dùng KeyOnlyStringProvider → nhãn là khoá resource.
        Assert.Equal("ParamFindReplacePattern", pattern.Label);
    }

    [Fact]
    public void ChoiceOptions_AreLocalizedWithTheirRawValue()
    {
        var vm = CreateViewModel();
        vm.AddRule("FindReplace");

        var scope = vm.Rules[0].Parameters.First(p => p.Key == "scope");

        Assert.Equal(3, scope.Options.Count);
        Assert.Equal("Name", scope.Options[0].Value);
        Assert.Equal("OptionName", scope.Options[0].Label);
    }
}
