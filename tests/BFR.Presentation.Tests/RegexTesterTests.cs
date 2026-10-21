namespace BFR.Presentation.Tests;

using BFR.Core;
using BFR.Presentation;

using System;
using System.Linq;

using Xunit;

/// <summary>
/// Test cho Regex Probe (BFR.Core) và Trình thử Regex (BFR.Presentation) — mục A1.
/// </summary>
public class RegexTesterTests
{
    // ----------------------------------------------------------------- RegexProbe (Core)

    [Fact]
    public void Evaluate_SimplePattern_ReturnsMatchAndResult()
    {
        var result = RegexProbe.Evaluate(@"^(\d+)_(.+)$", "$2_$1", "2024_file.txt");

        Assert.True(result.IsValid);
        Assert.True(result.HasMatch);
        Assert.Equal("file.txt_2024", result.Output);
        Assert.Single(result.Matches);
    }

    [Fact]
    public void Evaluate_ExposesCaptureGroups()
    {
        var result = RegexProbe.Evaluate(@"^(\d+)_(.+)$", "$0", "2024_file.txt");

        var match = Assert.Single(result.Matches);
        Assert.Equal("2024_file.txt", match.Value);
        Assert.Equal(3, match.Groups.Count);
        Assert.Equal("2024", match.Groups[1]);
        Assert.Equal("file.txt", match.Groups[2]);
    }

    [Fact]
    public void Evaluate_NoMatch_ReturnsInputUnchanged()
    {
        var result = RegexProbe.Evaluate(@"^\d+$", "x", "abc");

        Assert.True(result.IsValid);
        Assert.False(result.HasMatch);
        Assert.Empty(result.Matches);
        Assert.Equal("abc", result.Output);
    }

    [Fact]
    public void Evaluate_InvalidPattern_ReportsError()
    {
        var result = RegexProbe.Evaluate("([unclosed", "$1", "abc");

        Assert.False(result.IsValid);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }

    [Fact]
    public void Evaluate_IgnoreCase_ChangesOutcome()
    {
        var sensitive = RegexProbe.Evaluate("abc", "x", "ABC");
        var insensitive = RegexProbe.Evaluate("abc", "x", "ABC", ignoreCase: true);

        Assert.False(sensitive.HasMatch);
        Assert.True(insensitive.HasMatch);
    }

    [Fact]
    public void Evaluate_EmptyPattern_IsInvalidWithoutError()
    {
        var result = RegexProbe.Evaluate(string.Empty, string.Empty, "abc");

        Assert.False(result.IsValid);
        Assert.True(string.IsNullOrEmpty(result.Error));
    }

    // ----------------------------------------------------------------- RegexTesterViewModel

    [Fact]
    public void ViewModel_RecalculatesOutputOnInputChange()
    {
        var vm = new RegexTesterViewModel(new KeyOnlyStringProvider(), new ImmediateUiDispatcher());

        vm.Pattern = @"^(\d+)_(\w+)$";
        vm.Replacement = "$2_$1";
        vm.Input = "2024_file";

        Assert.Equal("file_2024", vm.Output);
        Assert.False(vm.HasError);
        Assert.Single(vm.Matches);
    }

    [Fact]
    public void ViewModel_InvalidPattern_SetsErrorAndKeepsInput()
    {
        var vm = new RegexTesterViewModel(new KeyOnlyStringProvider(), new ImmediateUiDispatcher());
        vm.Input = "abc";

        vm.Pattern = "([unclosed";

        Assert.True(vm.HasError);
        Assert.False(string.IsNullOrWhiteSpace(vm.ErrorMessage));
        Assert.Equal("abc", vm.Output);
    }

    [Fact]
    public void ViewModel_ApplyToAllFiles_CallsCallbackWithValues()
    {
        string? appliedPattern = null;
        string? appliedReplacement = null;
        bool appliedIgnoreCase = false;
        bool appliedReplaceAll = false;

        var vm = new RegexTesterViewModel(
            new KeyOnlyStringProvider(),
            new ImmediateUiDispatcher(),
            (pattern, replacement, ignoreCase, replaceAll) =>
            {
                appliedPattern = pattern;
                appliedReplacement = replacement;
                appliedIgnoreCase = ignoreCase;
                appliedReplaceAll = replaceAll;
            });

        vm.Pattern = "abc";
        vm.Replacement = "xyz";
        vm.IgnoreCase = true;

        Assert.True(vm.ApplyToAllFiles());
        Assert.Equal("abc", appliedPattern);
        Assert.Equal("xyz", appliedReplacement);
        Assert.True(appliedIgnoreCase);
        Assert.True(appliedReplaceAll);
    }

    [Fact]
    public void ViewModel_ApplyToAllFiles_BlockedWhenPatternInvalid()
    {
        var called = false;
        var vm = new RegexTesterViewModel(
            new KeyOnlyStringProvider(),
            new ImmediateUiDispatcher(),
            (_, _, _, _) => called = true);

        vm.Pattern = "([unclosed";

        Assert.False(vm.ApplyToAllFiles());
        Assert.False(called);
    }

    [Fact]
    public void ViewModel_Samples_AreUsable()
    {
        var vm = new RegexTesterViewModel(new KeyOnlyStringProvider(), new ImmediateUiDispatcher());

        Assert.NotEmpty(vm.Samples);
        foreach (var sample in vm.Samples)
        {
            Assert.False(string.IsNullOrWhiteSpace(sample.Pattern));
            Assert.False(string.IsNullOrWhiteSpace(sample.TestInput));
        }
    }

    [Fact]
    public void ViewModel_SelectSample_LoadsPatternAndInput()
    {
        var vm = new RegexTesterViewModel(new KeyOnlyStringProvider(), new ImmediateUiDispatcher());
        var sample = vm.Samples.First();

        vm.SelectedSampleName = sample.Name;

        Assert.Equal(sample.Pattern, vm.Pattern);
        Assert.Equal(sample.TestInput, vm.Input);
    }
}
