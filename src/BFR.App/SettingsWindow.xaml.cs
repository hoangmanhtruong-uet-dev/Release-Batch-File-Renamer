namespace BFR.App;

using BFR.Core.Licensing;
using BFR.App.Infrastructure;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

/// <summary>
/// Trang Cài đặt (theo IA mục 4): ngôn ngữ, giao diện, cập nhật, giấy phép Pro, quyền riêng tư.
/// Thay cho các nút rải rác trên thanh công cụ trước đây.
/// </summary>
public class SettingsWindow : Window
{
    private readonly LicenseCoordinator _license;
    private readonly AppSettings _settings;

    public SettingsWindow(LicenseCoordinator license)
    {
        _license = license;
        _settings = SettingsStore.Load();

        Title = Loc.T("SettingsTitle");
        Width = 560;
        Height = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        AutomationProperties.SetName(this, "Settings window");
        Background = (Brush)Application.Current.FindResource("SurfaceBackground");

        var root = new DockPanel { Margin = new Thickness(20) };

        var content = new StackPanel();

        // ---- Ngôn ngữ ----
        content.Children.Add(Header(Loc.T("SettingsLanguage")));
        var language = new ComboBox { Width = 240, Style = (Style)Application.Current.FindResource("AppComboBoxStyle") };
        AutomationProperties.SetName(language, "Language selector");
        foreach (var code in new[] { "vi", "en", "fr", "zh-Hans" })
        {
            language.Items.Add(new ComboBoxItem { Content = LanguageName(code), Tag = code });
        }
        SelectByTag(language, _settings.Language);
        language.SelectionChanged += (_, _) =>
        {
            if (language.SelectedItem is ComboBoxItem { Tag: string code })
            {
                Loc.Apply(code);
                _settings.Language = code;
                SettingsStore.Save(_settings);
                UpdateColumnHeaders();
            }
        };
        content.Children.Add(language);

        // ---- Giao diện ----
        content.Children.Add(Header(Loc.T("SettingsTheme")));
        var theme = new ComboBox { Width = 240, Style = (Style)Application.Current.FindResource("AppComboBoxStyle") };
        AutomationProperties.SetName(theme, "Theme selector");
        theme.Items.Add(new ComboBoxItem { Content = Loc.T("ThemeSystem"), Tag = "System" });
        theme.Items.Add(new ComboBoxItem { Content = Loc.T("ThemeLight"), Tag = "Light" });
        theme.Items.Add(new ComboBoxItem { Content = Loc.T("ThemeDark"), Tag = "Dark" });
        SelectByTag(theme, _settings.Theme);
        theme.SelectionChanged += (_, _) =>
        {
            if (theme.SelectedItem is ComboBoxItem { Tag: string tag }
                && Enum.TryParse<ThemeManager.AppTheme>(tag, out var value))
            {
                _settings.Theme = tag;
                SettingsStore.Save(_settings);
                ThemeManager.Apply(value);
            }
        };
        content.Children.Add(theme);

        // ---- Cập nhật ----
        content.Children.Add(Header(Loc.T("SettingsUpdates")));
        var checkUpdates = new CheckBox
        {
            Content = Loc.T("SettingsCheckUpdates"),
            IsChecked = _settings.CheckForUpdates,
            Style = (Style)Application.Current.FindResource("AppCheckBoxStyle"),
            Margin = new Thickness(0, 0, 0, 8)
        };
        checkUpdates.Checked += (_, _) => { _settings.CheckForUpdates = true; SettingsStore.Save(_settings); };
        checkUpdates.Unchecked += (_, _) => { _settings.CheckForUpdates = false; SettingsStore.Save(_settings); };
        content.Children.Add(checkUpdates);

        content.Children.Add(Caption(Loc.T("SettingsUpdateUrl")));
        var url = new TextBox
        {
            Text = _settings.UpdateFeedUrl,
            Style = (Style)Application.Current.FindResource("AppTextBoxStyle")
        };
        AutomationProperties.SetName(url, "Update feed url");
        url.TextChanged += (_, _) => { _settings.UpdateFeedUrl = url.Text; SettingsStore.Save(_settings); };
        content.Children.Add(url);

        // Nút kiểm tra ngay + kết quả hiển thị tại chỗ
        var checkNow = new Button
        {
            Content = Loc.T("SettingsCheckUpdateNow"),
            Style = (Style)Application.Current.FindResource("SecondaryButtonStyle"),
            Padding = new Thickness(14, 6, 14, 6),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 4, 0, 0)
        };
        AutomationProperties.SetName(checkNow, "Check for updates now");
        content.Children.Add(checkNow);

        var updateStatus = new TextBlock
        {
            Text = string.Empty,
            Foreground = (Brush)Application.Current.FindResource("TextSecondary"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 8)
        };
        AutomationProperties.SetName(updateStatus, "Update check result");
        content.Children.Add(updateStatus);

        checkNow.Click += async (_, _) =>
        {
            checkNow.IsEnabled = false;
            checkNow.Content = Loc.T("SettingsUpdateChecking");
            updateStatus.Text = Loc.T("SettingsUpdateChecking");

            try
            {
                var result = await UpdateRunner.CheckAsync();
                updateStatus.Text = result.Message;

                if (result.Status == UpdateCheckStatus.UpdateAvailable
                    && !string.IsNullOrWhiteSpace(result.ReleaseUrl))
                {
                    var open = new Button
                    {
                        Content = Loc.T("UpdateDownload"),
                        Style = (Style)Application.Current.FindResource("PrimaryButtonStyle"),
                        Padding = new Thickness(14, 6, 14, 6),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = new Thickness(0, 4, 0, 0)
                    };
                    open.Click += (_, _) =>
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = result.ReleaseUrl,
                                UseShellExecute = true
                            });
                        }
                        catch
                        {
                            // Bỏ qua nếu không mở được trình duyệt.
                        }
                    };
                    content.Children.Insert(content.Children.Count - 1, open);
                }
            }
            finally
            {
                checkNow.IsEnabled = true;
                checkNow.Content = Loc.T("SettingsCheckUpdateNow");
            }
        };

        // ---- Giấy phép ----
        content.Children.Add(Header(Loc.T("SettingsLicense")));
        var status = _license.Status;
        var licenseText = status.IsTrial
            ? Loc.F("SettingsLicenseTrial", (int)Math.Ceiling((_license.License.TrialRemaining() ?? TimeSpan.Zero).TotalDays))
            : status.IsPro
                ? Loc.T("SettingsLicensePro")
                : Loc.T("SettingsLicenseFree");

        content.Children.Add(new TextBlock
        {
            Text = licenseText,
            Foreground = (Brush)Application.Current.FindResource("TextSecondary"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        });

        if (!status.IsPro)
        {
            var upgrade = new Button
            {
                Content = Loc.T("Upgrade"),
                Padding = new Thickness(16, 6, 16, 6),
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = (Brush)Application.Current.FindResource("Accent"),
                Foreground = (Brush)Application.Current.FindResource("TextOnAccent"),
                BorderThickness = new Thickness(0)
            };
            upgrade.Click += (_, _) => { Close(); };
            content.Children.Add(upgrade);
        }

        // ---- Quyền riêng tư ----
        content.Children.Add(Header(Loc.T("SettingsPrivacy")));
        content.Children.Add(Caption(Loc.T("SettingsPrivacyHint")));

        content.Children.Add(Header(Loc.T("SettingsAbout")));
        content.Children.Add(Caption(Loc.F("SettingsVersion", "1.2.2")));

        var rateButton = new Button
        {
            Content = Loc.T("RateApp"),
            Style = (Style)Application.Current.FindResource("SecondaryButtonStyle"),
            Padding = new Thickness(14, 6, 14, 6),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 8, 0, 0)
        };
        AutomationProperties.SetName(rateButton, "Rate this app");
        rateButton.Click += async (_, _) =>
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            await BFR.Store.StoreRatingService.RequestRateAndReviewAsync(hwnd);
        };
        content.Children.Add(rateButton);

        var close = new Button
        {
            Content = Loc.T("SettingsClose"),
            Padding = new Thickness(18, 6, 18, 6),
            HorizontalAlignment = HorizontalAlignment.Right,
            IsCancel = true,
            Margin = new Thickness(0, 16, 0, 0)
        };
        close.Click += (_, _) => Close();
        AutomationProperties.SetName(close, "Close settings");
        DockPanel.SetDock(close, Dock.Bottom);
        root.Children.Add(close);

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        scroll.Content = content;
        root.Children.Add(scroll);

        Content = root;
    }

    private static void UpdateColumnHeaders()
    {
        // Cột bảng dùng DynamicResource nên tự cập nhật; hàm này chỉ để mở rộng về sau.
    }

    private static void SelectByTag(ComboBox combo, string? tag)
    {
        foreach (var item in combo.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Tag as string, tag, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedItem = item;
                return;
            }
        }

        combo.SelectedIndex = 0;
    }

    private static string LanguageName(string code) => code switch
    {
        "en" => "English",
        "fr" => "Français",
        "zh-Hans" => "简体中文",
        _ => "Tiếng Việt"
    };

    private static TextBlock Header(string text) => new()
    {
        Text = text,
        Style = (Style)Application.Current.FindResource("PanelHeaderTextStyle"),
        Margin = new Thickness(0, 14, 0, 6)
    };

    private static TextBlock Caption(string text) => new()
    {
        Text = text,
        Style = (Style)Application.Current.FindResource("CaptionTextStyle"),
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, 0, 0, 4)
    };
}
