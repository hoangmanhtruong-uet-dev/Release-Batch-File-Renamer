namespace BFR.App;

using BFR.Core;
using BFR.Core.Licensing;
using BFR.Core.WatchFolder;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;

/// <summary>
/// Cửa sổ Watch Folder (FR-D4, tính năng Pro của v1.2):
/// theo dõi một thư mục và tự áp dụng preset cho tệp mới.
/// </summary>
public class WatchFolderWindow : Window
{
    private readonly PresetStore _presetStore;
    private readonly LicenseService _license;
    private readonly RealFileSystem _fs = new();
    private readonly string _journalDir;
    private readonly string _logPath;

    private readonly TextBox _folderBox = new() { Width = 320 };
    private readonly ComboBox _presetCombo = new() { Width = 240, DisplayMemberPath = nameof(Preset.Name) };
    private readonly CheckBox _recursive = new() { IsChecked = true };
    private readonly CheckBox _dryRun = new();
    private readonly TextBox _settleBox = new() { Text = "1500", Width = 80 };
    private readonly TextBox _includeBox = new() { Width = 140 };
    private readonly TextBox _excludeBox = new() { Width = 140 };
    private readonly ListBox _logList = new();
    private readonly TextBlock _status = new() { Margin = new Thickness(0, 6, 0, 0) };
    private readonly TextBlock _logEmpty = new();
    private readonly Button _startButton = new();
    private readonly Button _stopButton = new();

    private WatchFolderService? _service;

    public WatchFolderWindow(PresetStore presetStore, LicenseService license)
    {
        _presetStore = presetStore;
        _license = license;

        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BatchFileRenamer");
        _journalDir = Path.Combine(appData, "journal");
        _logPath = Path.Combine(appData, "watch", "watch.log");

        Title = Loc.T("WatchTitle");
        Width = 800;
        Height = 620;
        MinHeight = 460;
        MinWidth = 620;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        AutomationProperties.SetName(this, "Watch folder window");

        Background = (System.Windows.Media.Brush)Application.Current.FindResource("AppBackground");
        FontFamily = (System.Windows.Media.FontFamily)Application.Current.FindResource("AppFont");
        FontSize = (double)Application.Current.FindResource("AppFontSizeBody");

        ApplyStyles();

        var root = new DockPanel { Margin = new Thickness(16) };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0)
        };
        var closeButton = MakeButton(Loc.T("CloseDialog"), "SecondaryButtonStyle");
        closeButton.IsCancel = true;
        closeButton.Click += (_, _) => { Stop(); DialogResult = false; Close(); };
        buttons.Children.Add(_startButton);
        buttons.Children.Add(_stopButton);
        buttons.Children.Add(closeButton);

        var options = new StackPanel();
        options.Children.Add(Header(Loc.T("WatchOptionsTitle")));
        options.Children.Add(Row(Loc.T("WatchFolder"), _folderBox, BrowseButton()));
        options.Children.Add(Row(Loc.T("WatchPreset"), _presetCombo));
        options.Children.Add(Row(Loc.T("WatchSettle"), _settleBox));
        options.Children.Add(Row(Loc.T("WatchInclude"), _includeBox));
        options.Children.Add(Row(Loc.T("WatchExclude"), _excludeBox));
        options.Children.Add(_recursive);
        options.Children.Add(_dryRun);

        _startButton.Click += (_, _) => Start();
        _stopButton.Click += (_, _) => Stop();

        _logList.ItemsSource = new List<string>();
        AutomationProperties.SetName(_logList, "Watch folder log");

        _logEmpty.Text = Loc.T("WatchFolderEmpty");
        _logEmpty.TextWrapping = TextWrapping.Wrap;
        _logEmpty.TextAlignment = TextAlignment.Center;
        _logEmpty.HorizontalAlignment = HorizontalAlignment.Center;
        _logEmpty.VerticalAlignment = VerticalAlignment.Center;
        _logEmpty.Margin = new Thickness(24);
        _logEmpty.Visibility = Visibility.Visible;
        _logEmpty.Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("TextSecondary");
        AutomationProperties.SetName(_logEmpty, "Watch folder empty");

        var logGrid = new Grid { Height = 240 };
        logGrid.Children.Add(_logList);
        logGrid.Children.Add(_logEmpty);

        var content = new DockPanel { Margin = new Thickness(0, 12, 0, 0) };
        DockPanel.SetDock(_status, Dock.Bottom);
        content.Children.Add(_status);
        content.Children.Add(logGrid);

        var body = new DockPanel();
        DockPanel.SetDock(options, Dock.Top);
        body.Children.Add(options);
        body.Children.Add(content);

        root.Children.Add(buttons);
        root.Children.Add(body);
        DockPanel.SetDock(buttons, Dock.Bottom);
        Content = root;

        RefreshPresets();
        _stopButton.IsEnabled = false;
    }

    /// <summary>Áp style/theme cho các điều khiển (đồng bộ với cửa sổ chính).</summary>
    private void ApplyStyles()
    {
        _folderBox.Style = (Style)Application.Current.FindResource("AppTextBoxStyle");
        _settleBox.Style = (Style)Application.Current.FindResource("AppTextBoxStyle");
        _includeBox.Style = (Style)Application.Current.FindResource("AppTextBoxStyle");
        _excludeBox.Style = (Style)Application.Current.FindResource("AppTextBoxStyle");
        _presetCombo.Style = (Style)Application.Current.FindResource("AppComboBoxStyle");
        _logList.Style = (Style)Application.Current.FindResource("AppListBoxStyle");
        _recursive.Style = (Style)Application.Current.FindResource("AppCheckBoxStyle");
        _dryRun.Style = (Style)Application.Current.FindResource("AppCheckBoxStyle");
        _recursive.Content = Loc.T("WatchRecursive");
        _dryRun.Content = Loc.T("WatchDryRun");
        _status.Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("TextSecondary");

        _startButton.Content = Loc.T("WatchStart");
        _startButton.Style = (Style)Application.Current.FindResource("PrimaryButtonStyle");
        _startButton.Margin = new Thickness(0, 0, 6, 0);
        _startButton.Padding = new Thickness(12, 6, 12, 6);

        _stopButton.Content = Loc.T("WatchStop");
        _stopButton.Style = (Style)Application.Current.FindResource("SecondaryButtonStyle");
        _stopButton.Margin = new Thickness(0, 0, 6, 0);
        _stopButton.Padding = new Thickness(12, 6, 12, 6);
    }

    private static TextBlock Header(string text) => new()
    {
        Text = text,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 0, 0, 8),
        Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("TextPrimary")
    };

    private static Button MakeButton(string content, string styleKey) => new()
    {
        Content = content,
        Style = (Style)Application.Current.FindResource(styleKey),
        Margin = new Thickness(0, 0, 6, 0),
        Padding = new Thickness(12, 6, 12, 6)
    };

    private static StackPanel Row(string label, UIElement element, UIElement? extra = null)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 3) };
        panel.Children.Add(new TextBlock
        {
            Text = label,
            Width = 150,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("TextSecondary")
        });
        panel.Children.Add(element);
        if (extra is not null) panel.Children.Add(extra);
        return panel;
    }

    private Button BrowseButton()
    {
        var button = new Button
        {
            Content = "…",
            Margin = new Thickness(4, 0, 0, 0),
            Padding = new Thickness(8, 4, 8, 4),
            Style = (Style)Application.Current.FindResource("SecondaryButtonStyle"),
            ToolTip = Loc.T("WatchBrowseButton")
        };
        button.Click += (_, _) =>
        {
            var dialog = new OpenFolderDialog { Title = Loc.T("WatchFolder") };
            if (dialog.ShowDialog() == true) _folderBox.Text = dialog.FolderName;
        };
        return button;
    }

    private void RefreshPresets()
    {
        var presets = _presetStore.LoadAll().Concat(BuiltInPresets.All).ToList();
        _presetCombo.ItemsSource = presets;
        if (presets.Count > 0) _presetCombo.SelectedIndex = 0;
    }

    private void Start()
    {
        if (!_license.Allows(ProFeature.UnlimitedRecursion))
        {
            MessageBox.Show(FeatureMessages.For(ProFeature.UnlimitedRecursion), Loc.T("ProLockedTitle"),
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var folder = _folderBox.Text.Trim();
        if (folder.Length == 0 || !Directory.Exists(folder))
        {
            MessageBox.Show(Loc.T("WatchFolderMissing"), Loc.T("WatchTitle"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_presetCombo.SelectedItem is not Preset preset)
        {
            MessageBox.Show(Loc.T("WatchPresetMissing"), Loc.T("WatchTitle"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var options = new WatchFolderOptions
        {
            Folder = folder,
            PresetName = preset.Name,
            Recursive = _recursive.IsChecked == true,
            DryRun = _dryRun.IsChecked == true,
            SettleDelayMs = int.TryParse(_settleBox.Text, out var settle) ? settle : 1500,
            IncludeGlob = string.IsNullOrWhiteSpace(_includeBox.Text) ? null : _includeBox.Text.Trim(),
            ExcludeGlob = string.IsNullOrWhiteSpace(_excludeBox.Text) ? null : _excludeBox.Text.Trim()
        };

        try
        {
            Stop();

            var processor = new WatchFolderProcessor(_fs, _journalDir);
            _service = new WatchFolderService(options, preset, processor, _fs, AppendLog);
            _service.BatchProcessed += (_, result) => Dispatcher.Invoke(() =>
                _status.Text = Loc.F("WatchBatchDone", result.Renamed, result.Unchanged, result.Skipped, result.Failed));
            _service.Start();

            _startButton.IsEnabled = false;
            _stopButton.IsEnabled = true;
            _status.Text = Loc.F("WatchStarted", folder, preset.Name);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Loc.T("WatchTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Stop()
    {
        if (_service is null) return;

        _service.Dispose();
        _service = null;
        _startButton.IsEnabled = true;
        _stopButton.IsEnabled = false;
    }

    private void AppendLog(WatchFolderLogEntry entry)
    {
        var line = entry.Format();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
            File.AppendAllText(_logPath, line + Environment.NewLine);
        }
        catch
        {
            // Nhật ký là thông tin phụ: không làm hỏng chức năng.
        }

        Dispatcher.Invoke(() =>
        {
            if (_logList.ItemsSource is List<string> items)
            {
                items.Add(line);
                while (items.Count > 500) items.RemoveAt(0);
                _logEmpty.Visibility = items.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            }
        });
    }

    protected override void OnClosed(EventArgs e)
    {
        Stop();
        base.OnClosed(e);
    }
}
