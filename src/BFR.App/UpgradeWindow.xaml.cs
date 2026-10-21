namespace BFR.App;

using BFR.App.Infrastructure;
using BFR.Presentation;
using System;
using System.Windows;
using System.Windows.Input;

/// <summary>
/// Hộp thoại nâng cấp Pro (thay cho MessageBox).
///
/// - Có nút đóng (X), bấm Esc để đóng, Tab điều hướng, Enter kích hoạt hành động chính.
/// - Bố cục + nội dung do <see cref="UpgradeViewModel"/> quyết định (Free/Pro, trạng thái dùng thử).
/// - Dùng đọc được: mọi vùng có AutomationProperties.Name, lỗi kích hoạt có thông báo riêng.
/// </summary>
public partial class UpgradeWindow : Window
{
    private readonly UpgradeViewModel _vm;

    public UpgradeWindow(UpgradeViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        _vm.Upgraded += (_, _) =>
        {
            DialogResult = true;
            Close();
        };

        Loaded += async (_, _) => await _vm.LoadPriceAsync();

        // Esc đóng hộp thoại.
        InputBindings.Add(new KeyBinding(new RelayCommand(() => Close()), Key.Escape, ModifierKeys.None));
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            try { DragMove(); }
            catch (InvalidOperationException) { /* không kéo được thì bỏ qua */ }
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Buy_Click(object sender, RoutedEventArgs e) => _vm.BuyCommand.Execute(null);

    private void Trial_Click(object sender, RoutedEventArgs e) => _vm.TrialCommand.Execute(null);

    private void ShowLicenseKey_Click(object sender, RoutedEventArgs e) => _vm.ShowLicenseKeyCommand.Execute(null);

    private void ActivateKey_Click(object sender, RoutedEventArgs e) => _vm.ActivateKeyCommand.Execute(null);

    private void LicenseKey_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;

        _vm.ActivateKeyCommand.Execute(null);
        e.Handled = true;
    }
}
