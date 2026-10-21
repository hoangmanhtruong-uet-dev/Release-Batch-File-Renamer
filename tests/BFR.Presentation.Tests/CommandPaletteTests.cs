namespace BFR.Presentation.Tests;

using BFR.Presentation;

using System.Linq;

using Xunit;

/// <summary>
/// Test cho Command Palette (Ctrl+K) — mục A4.
/// </summary>
public class CommandPaletteTests
{
    private static CommandPaletteViewModel Create(out CommandRegistry registry)
    {
        registry = new CommandRegistry();
        registry.Register("Thêm tệp", "Tệp", () => { });
        registry.Register("Thêm thư mục", "Tệp", () => { });
        registry.Register("Mở lịch sử và hoàn tác", "Công cụ", () => { });
        registry.Register("Mở cài đặt", "Ứng dụng", () => { });
        registry.Register("Nâng cấp lên Pro", "Ứng dụng", () => { });

        return new CommandPaletteViewModel(registry, new KeyOnlyStringProvider(), new ImmediateUiDispatcher());
    }

    [Fact]
    public void EmptyQuery_ShowsAllCommands()
    {
        var vm = Create(out _);

        Assert.Equal(5, vm.Filtered.Count);
        Assert.True(vm.HasResults);
    }

    [Fact]
    public void Query_FiltersByTitle()
    {
        var vm = Create(out _);

        vm.Query = "cài đặt";

        Assert.Single(vm.Filtered);
        Assert.Equal("Mở cài đặt", vm.Filtered[0].Title);
    }

    [Fact]
    public void Query_IgnoresDiacritics()
    {
        var vm = Create(out _);

        vm.Query = "lich su";   // không dấu

        Assert.Single(vm.Filtered);
        Assert.Equal("Mở lịch sử và hoàn tác", vm.Filtered[0].Title);
    }

    [Fact]
    public void Query_NoMatch_ShowsEmptyState()
    {
        var vm = Create(out _);

        vm.Query = "zzzzzz";

        Assert.Empty(vm.Filtered);
        Assert.False(vm.HasResults);
    }

    [Fact]
    public void Query_MatchesInitials()
    {
        var vm = Create(out _);

        vm.Query = "tt";   // "Thêm tệp" → "tt"

        Assert.Contains(vm.Filtered, c => c.Title == "Thêm tệp");
    }

    [Fact]
    public void MoveSelection_WrapsAround()
    {
        var vm = Create(out _);

        vm.MoveSelection(-1);
        Assert.Equal(vm.Filtered.Count - 1, vm.SelectedIndex);

        vm.MoveSelection(1);
        Assert.Equal(0, vm.SelectedIndex);
    }

    [Fact]
    public void ExecuteSelected_RunsCommand()
    {
        var registry = new CommandRegistry();
        var executed = 0;
        registry.Register("Lệnh", "Nhóm", () => executed++);

        var vm = new CommandPaletteViewModel(registry, new KeyOnlyStringProvider(), new ImmediateUiDispatcher());

        Assert.True(vm.ExecuteSelected());
        Assert.Equal(1, executed);
    }

    [Fact]
    public void ExecuteSelected_ReturnsFalseWhenNoCommands()
    {
        var vm = new CommandPaletteViewModel(new CommandRegistry(), new KeyOnlyStringProvider(), new ImmediateUiDispatcher());

        Assert.False(vm.ExecuteSelected());
    }
}
