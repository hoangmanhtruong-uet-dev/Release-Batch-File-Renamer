namespace BFR.App;

using BFR.Core.Licensing;
using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

/// <summary>
/// Màn hình thông tin license (mở khi người dùng đã là Pro và bấm badge "Pro").
/// Không phải hộp thoại nâng cấp — chỉ hiển thị trạng thái và hành động bảo trì.
/// </summary>
public class LicenseInfoWindow : Window
{
    public LicenseInfoWindow(LicenseService license, LicenseStatus status)
    {
        Title = Loc.T("LicenseInfoTitle");
        Width = 520;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
        Background = (Brush)Application.Current.FindResource("SurfaceBackground");
        FontFamily = (FontFamily)Application.Current.FindResource("AppFont");
        FontSize = (double)Application.Current.FindResource("AppFontSizeBody");
        AutomationProperties.SetName(this, "License information");

        var root = new StackPanel { Margin = new Thickness(22, 22, 22, 22) };

        root.Children.Add(new TextBlock
        {
            Text = Loc.T("LicenseInfoTitle"),
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)Application.Current.FindResource("TextPrimary")
        });

        root.Children.Add(new TextBlock
        {
            Text = Loc.T("LicenseInfoSubtitle"),
            Margin = new Thickness(0, 4, 0, 14),
            Foreground = (Brush)Application.Current.FindResource("TextSecondary")
        });

        var rows = new StackPanel();
        AddRow(rows, Loc.T("LicenseInfoStatus"), Loc.T("LicenseInfoActive"));
        if (status.IsTrial && status.TrialEndsUtc is not null)
        {
            AddRow(rows, Loc.T("LicenseInfoTrial"), status.TrialEndsUtc.Value.ToLocalTime().ToString("dd/MM/yyyy"));
        }
        else if (license.PurchasedUtc is not null)
        {
            AddRow(rows, Loc.T("LicenseInfoPurchased"), license.PurchasedUtc.Value.ToLocalTime().ToString("dd/MM/yyyy"));
        }
        AddRow(rows, Loc.T("LicenseInfoSource"), license.ActivatedByKey ? Loc.T("LicenseInfoSourceKey") : Loc.T("LicenseInfoSourceStore"));
        root.Children.Add(rows);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 18, 0, 0)
        };

        var close = new Button
        {
            Content = Loc.T("SettingsClose"),
            Padding = new Thickness(18, 6, 18, 6),
            Background = (Brush)Application.Current.FindResource("Accent"),
            Foreground = (Brush)Application.Current.FindResource("TextOnAccent"),
            BorderThickness = new Thickness(0),
            IsDefault = true,
            IsCancel = true
        };
        close.Click += (_, _) => { DialogResult = true; Close(); };
        buttons.Children.Add(close);
        root.Children.Add(buttons);

        Content = root;
    }

    private static void AddRow(Panel parent, string label, string value)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        grid.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = (Brush)Application.Current.FindResource("TextSecondary")
        });
        grid.Children.Add(new TextBlock
        {
            Text = value,
            Foreground = (Brush)Application.Current.FindResource("TextPrimary")
        });
        Grid.SetColumn(grid.Children[1], 1);

        parent.Children.Add(grid);
    }
}
