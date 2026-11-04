namespace BFR.App;

using BFR.Core.Licensing;
using BFR.Presentation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

/// <summary>
/// Khay nổi: thả tệp từ nhiều thư mục vào đây rồi đổi tên cùng lúc.
/// Logic nằm trong <see cref="ShelfModel"/>; cửa sổ chỉ nối view với model.
/// </summary>
public partial class ShelfWindow : Window
{
    private sealed record ShelfRow(string Name, string FullPath);

    private readonly ShelfModel _model;
    private readonly Action<IReadOnlyList<string>> _sendToMain;
    private readonly Action<ProFeature> _onLimitReached;

    public ShelfWindow(
        LicenseService license,
        Action<IReadOnlyList<string>> sendToMain,
        Action<ProFeature> onLimitReached)
    {
        InitializeComponent();

        _model = new ShelfModel(license);
        _sendToMain = sendToMain;
        _onLimitReached = onLimitReached;

        Title = Loc.L("Khay tệp (Shelf)", "Shelf");
        ClearButton.Content = Loc.L("Xóa hết", "Clear");
        RenameButton.Content = Loc.L("Đổi tên…", "Rename…");
        HintText.Text = Loc.L(
            "Kéo tệp hoặc thư mục từ nhiều nơi vào đây,\nrồi bấm Đổi tên để xử lý cùng lúc.",
            "Drag files or folders here from anywhere,\nthen press Rename to process them together.");

        _model.Items.CollectionChanged += (_, _) => Refresh();
        Refresh();
    }

    /// <summary>Thêm đường dẫn từ bên ngoài (ví dụ lệnh trong cửa sổ chính).</summary>
    public void AddPaths(IEnumerable<string> paths)
    {
        var result = _model.Add(paths);
        if (result.HitLimit) _onLimitReached(ProFeature.UnlimitedShelf);
    }

    private void Refresh()
    {
        ItemList.ItemsSource = _model.Items
            .Select(p => new ShelfRow(Path.GetFileName(p.TrimEnd('\\', '/')) is { Length: > 0 } n ? n : p, p))
            .ToList();

        var limit = _model.Limit == int.MaxValue ? "∞" : _model.Limit.ToString();
        CountText.Text = $"{_model.Count} / {limit}";
        HintText.Visibility = _model.IsEmpty ? Visibility.Visible : Visibility.Collapsed;
        RenameButton.IsEnabled = !_model.IsEmpty;
        ClearButton.IsEnabled = !_model.IsEmpty;
    }

    private void Shelf_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Shelf_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths) AddPaths(paths);
    }

    private void ItemList_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Delete) return;

        foreach (var row in ItemList.SelectedItems.OfType<ShelfRow>().ToList())
        {
            _model.Remove(row.FullPath);
        }
        e.Handled = true;
    }

    private void Clear_Click(object sender, RoutedEventArgs e) => _model.Clear();

    private void Rename_Click(object sender, RoutedEventArgs e)
    {
        if (_model.IsEmpty) return;

        _sendToMain(_model.Snapshot());
        _model.Clear();
    }
}
