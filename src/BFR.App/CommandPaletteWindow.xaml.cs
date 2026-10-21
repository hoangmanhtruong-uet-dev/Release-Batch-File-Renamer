namespace BFR.App;

using System.Windows;
using System.Windows.Input;

using BFR.Presentation;

/// <summary>
/// Command Palette (Ctrl+K): gõ để tìm lệnh, ↑↓ để chọn, Enter để chạy, Esc để đóng.
/// </summary>
public partial class CommandPaletteWindow : Window
{
    private readonly CommandPaletteViewModel _vm;

    public CommandPaletteWindow(CommandPaletteViewModel vm, Window? owner = null)
    {
        _vm = vm;

        InitializeComponent();
        DataContext = vm;

        if (owner is not null) Owner = owner;

        InputBindings.Add(new KeyBinding(new RelayCommand(() => Close()), Key.Escape, ModifierKeys.None));
        InputBindings.Add(new KeyBinding(new RelayCommand(() => _vm.MoveSelection(-1)), Key.Up, ModifierKeys.None));
        InputBindings.Add(new KeyBinding(new RelayCommand(() => _vm.MoveSelection(1)), Key.Down, ModifierKeys.None));
        InputBindings.Add(new KeyBinding(new RelayCommand(RunSelected), Key.Enter, ModifierKeys.None));

        Loaded += (_, _) =>
        {
            if (InputBox != null)
            {
                InputBox.Focus();
                InputBox.SelectAll();
            }
        };
    }

    private void RunSelected()
    {
        if (_vm.ExecuteSelected())
        {
            Close();
        }
    }
}
