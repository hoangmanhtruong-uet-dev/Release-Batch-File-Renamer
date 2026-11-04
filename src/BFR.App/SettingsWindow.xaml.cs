namespace BFR.App;

using BFR.Core.Licensing;
using BFR.App.Infrastructure;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using Ui = Wpf.Ui.Controls;

/// <summary>
/// Trang Cài đặt (theo IA mục 4): ngôn ngữ, giao diện, Shelf/chạy nền, cập nhật,
/// giấy phép Pro, quyền riêng tư. Dựng bằng WPF-UI: FluentWindow + TitleBar,
/// mỗi nhóm là một Card, tuỳ chọn bật/tắt dùng ToggleSwitch.
/// </summary>
public class SettingsWindow : Ui.FluentWindow
{
    private readonly LicenseCoordinator _license;
    private readonly AppSettings _settings;

    public SettingsWindow(LicenseCoordinator license)
    {
        _license = license;
        _settings = SettingsStore.Load();

        Title = Loc.T("SettingsTitle");
        Width = 600;
        Height = 660;
        MinHeight = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        ExtendsContentIntoTitleBar = true;
        WindowBackdropType = Ui.WindowBackdropType.Mica;
        AutomationProperties.SetName(this, "Settings window");

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var titleBar = new Ui.TitleBar { Title = Title, ShowMinimize = false, ShowMaximize = false };
        Grid.SetRow(titleBar, 0);
        root.Children.Add(titleBar);

        var content = new StackPanel { Margin = new Thickness(20, 4, 20, 8) };

        // ---- Giao diện & ngôn ngữ ----
        var look = Card(content, Loc.T("SettingsLanguage") + " · " + Loc.T("SettingsTheme"));

        var language = new ComboBox { MinWidth = 240, HorizontalAlignment = HorizontalAlignment.Left };
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
            }
        };
        look.Children.Add(Caption(Loc.T("SettingsLanguage")));
        look.Children.Add(language);

        var theme = new ComboBox { MinWidth = 240, HorizontalAlignment = HorizontalAlignment.Left };
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
        var themeCaption = Caption(Loc.T("SettingsTheme"));
        themeCaption.Margin = new Thickness(0, 12, 0, 4);
        look.Children.Add(themeCaption);
        look.Children.Add(theme);

        // ---- Shelf & chạy nền ----
        var shelf = Card(content, Loc.T("SettingsShelf"));
        shelf.Children.Add(Toggle(
            Loc.T("SettingsShake"),
            _settings.ShakeToOpenShelf,
            on => _settings.ShakeToOpenShelf = on));
        shelf.Children.Add(Caption(Loc.T("SettingsShakeHint")));
        var bg = Toggle(
            Loc.T("SettingsRunInBackground"),
            _settings.RunInBackground,
            on => _settings.RunInBackground = on);
        bg.Margin = new Thickness(0, 12, 0, 0);
        shelf.Children.Add(bg);

        // ---- Cập nhật ----
        var updates = Card(content, Loc.T("SettingsUpdates"));
        updates.Children.Add(Toggle(Loc.T("SettingsCheckUpdates"), _settings.CheckForUpdates,
            on => _settings.CheckForUpdates = on));

        var urlCaption = Caption(Loc.T("SettingsUpdateUrl"));
        urlCaption.Margin = new Thickness(0, 12, 0, 4);
        updates.Children.Add(urlCaption);
        var url = new TextBox { Text = _settings.UpdateFeedUrl };
        AutomationProperties.SetName(url, "Update feed url");
        url.TextChanged += (_, _) => { _settings.UpdateFeedUrl = url.Text; SettingsStore.Save(_settings); };
        updates.Children.Add(url);

        var checkNow = new Ui.Button
        {
            Content = Loc.T("SettingsCheckUpdateNow"),
            Appearance = Ui.ControlAppearance.Secondary,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 10, 0, 0)
        };
        AutomationProperties.SetName(checkNow, "Check for updates now");
        updates.Children.Add(checkNow);

        var updateStatus = new TextBlock
        {
            Text = string.Empty,
            Foreground = (Brush)Application.Current.FindResource("TextSecondary"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0)
        };
        AutomationProperties.SetName(updateStatus, "Update check result");
        updates.Children.Add(updateStatus);

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
                    var open = new Ui.Button
                    {
                        Content = Loc.T("UpdateDownload"),
                        Appearance = Ui.ControlAppearance.Primary,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = new Thickness(0, 8, 0, 0)
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
                    updates.Children.Add(open);
                }
            }
            finally
            {
                checkNow.IsEnabled = true;
                checkNow.Content = Loc.T("SettingsCheckUpdateNow");
            }
        };

        // ---- Giấy phép ----
        var licenseCard = Card(content, Loc.T("SettingsLicense"));
        var status = _license.Status;
        var licenseText = status.IsTrial
            ? Loc.F("SettingsLicenseTrial", (int)Math.Ceiling((_license.License.TrialRemaining() ?? TimeSpan.Zero).TotalDays))
            : status.IsPro
                ? Loc.T("SettingsLicensePro")
                : Loc.T("SettingsLicenseFree");

        licenseCard.Children.Add(new TextBlock
        {
            Text = licenseText,
            Foreground = (Brush)Application.Current.FindResource("TextSecondary"),
            TextWrapping = TextWrapping.Wrap
        });

        if (!status.IsPro)
        {
            var upgrade = new Ui.Button
            {
                Content = Loc.T("Upgrade"),
                Appearance = Ui.ControlAppearance.Primary,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 10, 0, 0)
            };
            upgrade.Click += (_, _) => { Close(); };
            licenseCard.Children.Add(upgrade);
        }

        // ---- Quyền riêng tư & giới thiệu ----
        var about = Card(content, Loc.T("SettingsPrivacy") + " · " + Loc.T("SettingsAbout"));
        about.Children.Add(Caption(Loc.T("SettingsPrivacyHint")));
        about.Children.Add(Caption(Loc.F("SettingsVersion", BFR.Core.AppVersion.Current)));

        var rateButton = new Ui.Button
        {
            Content = Loc.T("RateApp"),
            Appearance = Ui.ControlAppearance.Secondary,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 8, 0, 0)
        };
        AutomationProperties.SetName(rateButton, "Rate this app");
        rateButton.Click += async (_, _) =>
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            await BFR.Store.StoreRatingService.RequestRateAndReviewAsync(hwnd);
        };
        about.Children.Add(rateButton);

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = content };
        Grid.SetRow(scroll, 1);
        root.Children.Add(scroll);

        var close = new Ui.Button
        {
            Content = Loc.T("SettingsClose"),
            HorizontalAlignment = HorizontalAlignment.Right,
            IsCancel = true,
            Margin = new Thickness(20, 8, 20, 16),
            MinWidth = 96
        };
        close.Click += (_, _) => Close();
        AutomationProperties.SetName(close, "Close settings");
        Grid.SetRow(close, 2);
        root.Children.Add(close);

        Content = root;
    }

    /// <summary>Thêm một Card có tiêu đề vào <paramref name="parent"/> và trả về StackPanel bên trong.</summary>
    private static StackPanel Card(StackPanel parent, string title)
    {
        var inner = new StackPanel();
        inner.Children.Add(new TextBlock
        {
            Text = title,
            Style = (Style)Application.Current.FindResource("PanelHeaderTextStyle"),
            Margin = new Thickness(0, 0, 0, 8)
        });

        parent.Children.Add(new Ui.Card
        {
            Content = inner,
            Margin = new Thickness(0, 0, 0, 12),
            Padding = new Thickness(16),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        });
        return inner;
    }

    /// <summary>Công tắc bật/tắt: lưu ngay vào cài đặt khi đổi.</summary>
    private Ui.ToggleSwitch Toggle(string text, bool isOn, Action<bool> set)
    {
        // Content là TextBlock có xuống dòng: ToggleSwitch không tự wrap chuỗi dài (tiếng Pháp, tiếng Trung).
        var label = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, MaxWidth = 460 };
        var toggle = new Ui.ToggleSwitch { Content = label, IsChecked = isOn };
        AutomationProperties.SetName(toggle, text);
        toggle.Checked += (_, _) => { set(true); SettingsStore.Save(_settings); };
        toggle.Unchecked += (_, _) => { set(false); SettingsStore.Save(_settings); };
        return toggle;
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

    private static TextBlock Caption(string text) => new()
    {
        Text = text,
        Style = (Style)Application.Current.FindResource("CaptionTextStyle"),
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, 0, 0, 4)
    };
}
