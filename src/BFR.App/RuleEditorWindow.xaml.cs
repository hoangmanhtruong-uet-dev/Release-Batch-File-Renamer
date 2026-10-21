namespace BFR.App;

using BFR.Core;
using BFR.Core.Licensing;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

public partial class RuleEditorWindow : Window
{
    private readonly Preset _preset;
    private readonly List<RuleDefinition> _rules;
    private readonly LicenseService _license;

    public Preset EditedPreset => _preset;

    public RuleEditorWindow(Preset preset, LicenseService? license = null)
    {
        InitializeComponent();

        _preset = JsonSerializer.Deserialize<Preset>(JsonSerializer.Serialize(preset)) ?? new Preset();
        _preset.Rules ??= new List<RuleDefinition>();
        _rules = _preset.Rules;
        _license = license ?? new LicenseService(new LicenseStore(new RealFileSystem()), trialEnabled: false);

        NewTypeCombo.ItemsSource = new[]
        {
            "FindReplace", "Numbering", "Trim", "Insert", "Case",
            "Sanitize", "Date", "Extension", "Template"
        };
        NewTypeCombo.SelectedIndex = 0;

        RefreshList();
    }

    private void RefreshList()
    {
        RuleList.ItemsSource = null;
        RuleList.ItemsSource = _rules;
        if (_rules.Count > 0) RuleList.SelectedIndex = 0;
    }

    private void RuleList_SelectionChanged(object sender, SelectionChangedEventArgs e) => ShowEditor();

    private static string L(string vi, string en) => Loc.L(vi, en);

    private void ShowEditor()
    {
        EditorPanel.Children.Clear();
        if (RuleList.SelectedItem is not RuleDefinition rule) return;

        AddReadOnly(L("Loại quy tắc", "Rule type"), rule.Type);
        AddText(L("Tên hiển thị", "Display name"), rule.Name ?? string.Empty, v => rule.Name = string.IsNullOrWhiteSpace(v) ? null : v);

        switch (rule.Type)
        {
            case "FindReplace":
                AddText(L("Mẫu (pattern)", "Pattern"), GetString(rule, "pattern") ?? "", v => Set(rule, "pattern", v));
                AddText(L("Thay bằng", "Replace with"), GetString(rule, "replacement") ?? "", v => Set(rule, "replacement", v));
                AddCheck(L("Dùng Regex", "Use regex"), GetBool(rule, "isRegex"), v => Set(rule, "isRegex", v));
                AddCheck(L("Không phân biệt hoa/thường", "Ignore case"), GetBool(rule, "ignoreCase"), v => Set(rule, "ignoreCase", v));
                AddCheck(L("Thay tất cả", "Replace all"), GetBool(rule, "replaceAll", true), v => Set(rule, "replaceAll", v));
                AddCombo(L("Phạm vi", "Scope"), new[] { "Name", "Extension", "Both" }, GetString(rule, "scope") ?? "Name", v => Set(rule, "scope", v));
                break;

            case "Numbering":
                AddInt(L("Bắt đầu", "Start"), GetInt(rule, "start", 1), v => Set(rule, "start", v));
                AddInt(L("Bước nhảy", "Step"), GetInt(rule, "step", 1), v => Set(rule, "step", v));
                AddInt(L("Độ rộng đệm 0", "Zero-padding width"), GetInt(rule, "width", 3), v => Set(rule, "width", v));
                AddCombo(L("Vị trí", "Position"), new[] { "Start", "End", "ReplaceAll", "Index" }, GetString(rule, "position") ?? "Start", v => Set(rule, "position", v));
                AddText(L("Dấu ngăn", "Separator"), GetString(rule, "separator") ?? "_", v => Set(rule, "separator", v));
                AddCombo(L("Kiểu số", "Number style"), NumberKindOptions(), GetString(rule, "kind") ?? "Decimal", v => Set(rule, "kind", v));
                break;

            case "Trim":
                AddText(L("Xóa ký tự (tập)", "Characters to remove"), GetString(rule, "removeChars") ?? "", v => Set(rule, "removeChars", v));
                AddCheck(L("Gộp khoảng trắng", "Collapse spaces"), GetBool(rule, "collapseSpaces"), v => Set(rule, "collapseSpaces", v));
                AddCheck(L("Cắt khoảng trắng đầu/cuối", "Trim leading/trailing spaces"), GetBool(rule, "trimEnds"), v => Set(rule, "trimEnds", v));
                AddCheck(L("Xóa dấu chấm/khoảng trắng cuối", "Remove trailing dot/space"), GetBool(rule, "removeTrailingDotSpace"), v => Set(rule, "removeTrailingDotSpace", v));
                AddInt(L("Xóa N ký tự đầu", "Remove first N chars"), GetInt(rule, "removeFirstN", 0), v => Set(rule, "removeFirstN", v));
                AddInt(L("Xóa N ký tự cuối", "Remove last N chars"), GetInt(rule, "removeLastN", 0), v => Set(rule, "removeLastN", v));
                break;

            case "Insert":
                AddText(L("Văn bản/token", "Text/token"), GetString(rule, "text") ?? "", v => Set(rule, "text", v));
                AddCombo(L("Vị trí", "Position"), new[] { "Start", "End", "AtIndex" }, GetString(rule, "position") ?? "Start", v => Set(rule, "position", v));
                AddInt(L("Vị trí N", "Index N"), GetInt(rule, "index", 0), v => Set(rule, "index", v));
                AddCheck(L("Đếm từ cuối", "Count from end"), GetBool(rule, "countFromEnd"), v => Set(rule, "countFromEnd", v));
                break;

            case "Case":
                AddCombo(L("Kiểu", "Case style"), new[] { "Lower", "Upper", "Title", "Sentence" }, GetString(rule, "mode") ?? "Lower", v => Set(rule, "mode", v));
                AddCombo(L("Phạm vi", "Scope"), new[] { "Name", "Extension", "Both" }, GetString(rule, "scope") ?? "Name", v => Set(rule, "scope", v));
                AddText(L("Từ ngoại lệ (phân cách phẩy)", "Exceptions (comma-separated)"), GetString(rule, "exceptions") ?? "", v => Set(rule, "exceptions", v.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)));
                break;

            case "Sanitize":
                AddCheck(L("Bỏ dấu tiếng Việt", "Remove Vietnamese diacritics"), GetBool(rule, "removeDiacritics"), v => Set(rule, "removeDiacritics", v));
                AddCheck(L("Chuẩn hóa Unicode NFC", "Normalize Unicode NFC"), GetBool(rule, "normalizeNfc"), v => Set(rule, "normalizeNfc", v));
                AddCheck(L("Thay ký tự không hợp lệ", "Replace invalid characters"), GetBool(rule, "replaceInvalidChars"), v => Set(rule, "replaceInvalidChars", v));
                AddCombo(L("Khoảng trắng", "Spaces"), new[] { "Keep", "Dash", "Underscore", "Remove" }, GetString(rule, "spaces") ?? "Keep", v => Set(rule, "spaces", v));
                AddCheck(L("Slug (a-z0-9-)", "Slug (a-z0-9-)"), GetBool(rule, "slugify"), v => Set(rule, "slugify", v));
                break;

            case "Date":
                // Taken (EXIF) và Media là tính năng Pro: bản Free chỉ có Created/Modified/Now.
                AddCombo(L("Nguồn", "Source"), DateSourceOptions(), GetString(rule, "source") ?? "Created", v => Set(rule, "source", v));
                if (!_license.Allows(ProFeature.ExifDate))
                {
                    EditorPanel.Children.Add(new TextBlock
                    {
                        Text = L("Nguồn ngày chụp EXIF / ngày tạo media là tính năng Pro.", "EXIF capture date / media creation date is a Pro feature."),
                        Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DarkOrange),
                        Margin = new Thickness(0, 2, 0, 2)
                    });
                }
                AddText(L("Định dạng", "Format"), GetString(rule, "format") ?? "yyyy-MM-dd", v => Set(rule, "format", v));
                AddCombo(L("Vị trí", "Position"), new[] { "Start", "End", "ReplaceAll" }, GetString(rule, "position") ?? "Start", v => Set(rule, "position", v));
                AddText(L("Dấu ngăn", "Separator"), GetString(rule, "separator") ?? "_", v => Set(rule, "separator", v));
                AddCheck(L("Dùng UTC", "Use UTC"), GetBool(rule, "useUtc"), v => Set(rule, "useUtc", v));
                break;

            case "Extension":
                AddCombo(L("Chế độ", "Mode"), new[] { "Keep", "Lower", "Upper", "Set", "Remove" }, GetString(rule, "mode") ?? "Keep", v => Set(rule, "mode", v));
                AddText(L("Đuôi mới", "New extension"), GetString(rule, "newExtension") ?? "", v => Set(rule, "newExtension", v));
                AddCheck(L("Cho phép đổi đuôi", "Allow extension change"), GetBool(rule, "allowExtensionChange"), v => Set(rule, "allowExtensionChange", v));
                break;

            case "Template":
                AddText(L("Mẫu tên", "Name template"), GetString(rule, "template") ?? "{name}", v => Set(rule, "template", v));
                break;
        }
    }

    private void AddReadOnly(string label, string value)
    {
        var panel = Row(label);
        panel.Children.Add(new TextBlock { Text = value, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
        EditorPanel.Children.Add(panel);
    }

    private void AddText(string label, string value, Action<string> onChanged)
    {
        var panel = Row(label);
        var box = new TextBox { Text = value, Width = 360 };
        box.TextChanged += (_, _) => onChanged(box.Text);
        panel.Children.Add(box);
        EditorPanel.Children.Add(panel);
    }

    private void AddInt(string label, int value, Action<int> onChanged)
    {
        AddText(label, value.ToString(), v => { if (int.TryParse(v, out var n)) onChanged(n); });
    }

    private void AddCheck(string label, bool value, Action<bool> onChanged)
    {
        var check = new CheckBox { Content = label, IsChecked = value, Margin = new Thickness(0, 4, 0, 4) };
        check.Checked += (_, _) => onChanged(true);
        check.Unchecked += (_, _) => onChanged(false);
        EditorPanel.Children.Add(check);
    }

    private void AddCombo(string label, string[] options, string value, Action<string> onChanged)
    {
        var panel = Row(label);
        var combo = new ComboBox { ItemsSource = options, Width = 360, SelectedItem = value };
        combo.SelectionChanged += (_, _) => { if (combo.SelectedItem is string s) onChanged(s); };
        panel.Children.Add(combo);
        EditorPanel.Children.Add(panel);
    }

    private static StackPanel Row(string label)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
        panel.Children.Add(new TextBlock { Text = label, Width = 220, VerticalAlignment = VerticalAlignment.Center });
        return panel;
    }

    /// <summary>
    /// Danh sách nguồn ngày khả dụng theo cấp giấy phép (Taken/Media là Pro).
    /// </summary>
    private string[] DateSourceOptions()
    {
        if (_license.Allows(ProFeature.ExifDate)) return new[] { "Created", "Modified", "Taken", "Media", "Now" };
        return new[] { "Created", "Modified", "Now" };
    }

    /// <summary>Kiểu đánh số khả dụng (La Mã là Pro).</summary>
    private string[] NumberKindOptions()
    {
        if (_license.Allows(ProFeature.RomanNumbering)) return new[] { "Decimal", "Letters", "Roman" };
        return new[] { "Decimal", "Letters" };
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        // Bản Free chỉ cho phép tối đa 3 quy tắc trong một chuỗi (PHẦN 3.6).
        if (_rules.Count >= _license.MaxRules)
        {
            var gate = _license.Check(ProFeature.UnlimitedRules);
            MessageBox.Show(gate.Message, L("Tính năng Pro", "Pro feature"),
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var type = NewTypeCombo.SelectedItem as string ?? "FindReplace";
        var rule = new RuleDefinition { Type = type, Enabled = true };
        _rules.Add(rule);
        RefreshList();
        RuleList.SelectedItem = rule;
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (RuleList.SelectedItem is RuleDefinition rule)
        {
            _rules.Remove(rule);
            RefreshList();
        }
    }

    private void Up_Click(object sender, RoutedEventArgs e) => Move(-1);
    private void Down_Click(object sender, RoutedEventArgs e) => Move(+1);

    private void Move(int delta)
    {
        if (RuleList.SelectedItem is not RuleDefinition rule) return;
        var index = _rules.IndexOf(rule);
        var target = index + delta;
        if (target < 0 || target >= _rules.Count) return;
        _rules.RemoveAt(index);
        _rules.Insert(target, rule);
        RefreshList();
        RuleList.SelectedItem = rule;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private static string? GetString(RuleDefinition r, string key) =>
        r.Params.TryGetValue(key, out var e) && e.ValueKind == JsonValueKind.String ? e.GetString() : null;

    private static int GetInt(RuleDefinition r, string key, int fallback) =>
        r.Params.TryGetValue(key, out var e) && e.TryGetInt32(out var v) ? v : fallback;

    private static bool GetBool(RuleDefinition r, string key, bool fallback = false) =>
        r.Params.TryGetValue(key, out var e) ? e.ValueKind == JsonValueKind.True : fallback;

    private static void Set(RuleDefinition r, string key, object? value) =>
        r.Params[key] = JsonSerializer.SerializeToElement(value);
}
