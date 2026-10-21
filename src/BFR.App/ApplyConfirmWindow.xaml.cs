namespace BFR.App;

using BFR.Presentation;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

/// <summary>
/// Hộp thoại xác nhận áp dụng (spec 7.4): tóm tắt số tệp, số thư mục bị tác động,
/// số tệp đổi phần mở rộng và gợi ý rằng luôn có thể Hoàn tác.
/// </summary>
public class ApplyConfirmWindow : Window
{
    public ApplyConfirmWindow(ApplySummary summary)
    {
        Title = Loc.T("ConfirmDialogTitle");
        Width = 520;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        AutomationProperties.SetName(this, "Confirm apply renaming");
        Background = (Brush)Application.Current.FindResource("SurfaceBackground");

        var root = new StackPanel { Margin = new Thickness(20) };

        root.Children.Add(new TextBlock
        {
            Text = Loc.F("ConfirmDialogMessage", summary.FileCount),
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)Application.Current.FindResource("TextPrimary"),
            Margin = new Thickness(0, 0, 0, 10)
        });

        if (summary.FolderCount > 0)
        {
            root.Children.Add(Bullet(Loc.F("ConfirmFolders", summary.FolderCount)));
        }

        if (summary.ExtensionChangeCount > 0)
        {
            root.Children.Add(Bullet(Loc.F("ConfirmExtensions", summary.ExtensionChangeCount)));
        }

        root.Children.Add(Bullet(Loc.T("ConfirmUndoHint")));

        if (summary.Samples.Count > 0)
        {
            root.Children.Add(new TextBlock
            {
                Text = Loc.T("ConfirmSamples"),
                Style = (Style)Application.Current.FindResource("PanelHeaderTextStyle"),
                Margin = new Thickness(0, 12, 0, 4)
            });

            foreach (var (oldName, newName) in summary.Samples)
            {
                root.Children.Add(new TextBlock
                {
                    Text = $"{oldName}  →  {newName}",
                    FontFamily = (FontFamily)Application.Current.FindResource("AppFontMono"),
                    FontSize = 11.5,
                    Foreground = (Brush)Application.Current.FindResource("TextSecondary"),
                    Margin = new Thickness(0, 1, 0, 1)
                });
            }
        }

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 18, 0, 0)
        };

        var cancel = new Button
        {
            Content = Loc.T("ConfirmCancel"),
            Padding = new Thickness(18, 6, 18, 6),
            Margin = new Thickness(0, 0, 8, 0),
            IsCancel = true
        };
        cancel.Click += (_, _) => { DialogResult = false; Close(); };

        var apply = new Button
        {
            Content = Loc.T("ConfirmApplyButton"),
            Padding = new Thickness(18, 6, 18, 6),
            FontWeight = FontWeights.SemiBold,
            Background = (Brush)Application.Current.FindResource("Accent"),
            Foreground = (Brush)Application.Current.FindResource("TextOnAccent"),
            BorderThickness = new Thickness(0),
            IsDefault = true
        };
        apply.Click += (_, _) => { DialogResult = true; Close(); };

        buttons.Children.Add(cancel);
        buttons.Children.Add(apply);
        root.Children.Add(buttons);

        Content = root;
    }

    private static TextBlock Bullet(string text) => new()
    {
        Text = "• " + text,
        FontSize = 13,
        Foreground = (Brush)Application.Current.FindResource("TextSecondary"),
        Margin = new Thickness(0, 2, 0, 2)
    };
}
