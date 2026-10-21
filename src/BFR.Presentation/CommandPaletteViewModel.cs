namespace BFR.Presentation;

using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

/// <summary>Một lệnh trong Command Palette.</summary>
public sealed class PaletteCommand
{
    public PaletteCommand(string title, string category, Action execute, string? shortcut = null)
    {
        Title = title;
        Category = category;
        Execute = execute;
        Shortcut = shortcut;
    }

    /// <summary>Tên lệnh (đã địa phương hóa).</summary>
    public string Title { get; }

    /// <summary>Nhóm lệnh (đã địa phương hóa).</summary>
    public string Category { get; }

    /// <summary>Phím tắt hiển thị (ví dụ "Ctrl+K").</summary>
    public string? Shortcut { get; }

    /// <summary>Hành động khi chạy lệnh.</summary>
    public Action Execute { get; }
}

/// <summary>Kho lệnh của Command Palette.</summary>
public sealed class CommandRegistry
{
    private readonly List<PaletteCommand> _commands = new();

    /// <summary>Tất cả lệnh đã đăng ký.</summary>
    public IReadOnlyList<PaletteCommand> Commands => _commands;

    /// <summary>Đăng ký một lệnh.</summary>
    public CommandRegistry Register(string title, string category, Action execute, string? shortcut = null)
    {
        _commands.Add(new PaletteCommand(title, category, execute, shortcut));
        return this;
    }
}

/// <summary>
/// ViewModel cho Command Palette (Ctrl+K): gõ để lọc, ↑↓ để chọn, Enter để chạy.
/// </summary>
public sealed class CommandPaletteViewModel : ObservableObject
{
    private readonly CommandRegistry _registry;
    private readonly IStringProvider _strings;
    private readonly IUiDispatcher _dispatcher;

    private string _query = string.Empty;
    private PaletteCommand? _selected;
    private int _selectedIndex;

    public CommandPaletteViewModel(CommandRegistry registry, IStringProvider strings, IUiDispatcher dispatcher)
    {
        _registry = registry;
        _strings = strings;
        _dispatcher = dispatcher;
        Filtered = new ObservableCollection<PaletteCommand>();
        Refilter();
    }

    /// <summary>Các lệnh khớp với từ khoá.</summary>
    public ObservableCollection<PaletteCommand> Filtered { get; }

    /// <summary>Nhãn tìm kiếm.</summary>
    public string SearchHint => _strings.T("CommandPaletteSearch");

    /// <summary>Gợi ý phím tắt.</summary>
    public string HintText => _strings.T("CommandPaletteHint");

    /// <summary>Thông báo khi không có kết quả.</summary>
    public string EmptyText => _strings.T("CommandPaletteEmpty");

    /// <summary>True khi không có lệnh nào khớp.</summary>
    public bool HasResults => Filtered.Count > 0;

    /// <summary>Từ khoá tìm kiếm.</summary>
    public string Query
    {
        get => _query;
        set
        {
            if (Set(ref _query, value)) Refilter();
        }
    }

    /// <summary>Lệnh đang chọn.</summary>
    public PaletteCommand? Selected
    {
        get => _selected;
        set
        {
            if (!Set(ref _selected, value)) return;
            Raise(nameof(SelectedIndex));
        }
    }

    /// <summary>Chỉ số của lệnh đang chọn (dùng để highlight).</summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (!Set(ref _selectedIndex, value)) return;
            Raise(nameof(Selected));
        }
    }

    /// <summary>Di chuyển vùng chọn lên/xuống.</summary>
    public void MoveSelection(int delta)
    {
        if (Filtered.Count == 0) return;

        var index = _selectedIndex + delta;
        if (index < 0) index = Filtered.Count - 1;
        if (index >= Filtered.Count) index = 0;

        _selectedIndex = index;
        Raise(nameof(SelectedIndex));
        Selected = Filtered[index];
    }

    /// <summary>Chạy lệnh đang chọn (trả về false nếu không có lệnh nào).</summary>
    public bool ExecuteSelected()
    {
        var command = Selected ?? Filtered.FirstOrDefault();
        if (command is null) return false;

        command.Execute();
        return true;
    }

    private void Refilter()
    {
        var query = _query?.Trim() ?? string.Empty;

        var matches = _registry.Commands
            .Where(c => Matches(c, query))
            .OrderBy(c => c.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        _dispatcher.Post(() =>
        {
            Filtered.Clear();
            foreach (var command in matches)
            {
                Filtered.Add(command);
            }

            _selectedIndex = Filtered.Count > 0 ? 0 : -1;
            Selected = Filtered.FirstOrDefault();
            Raise(nameof(HasResults));
        });
    }

    private static bool Matches(PaletteCommand command, string query)
    {
        if (query.Length == 0) return true;

        var haystack = new StringBuilder(command.Title).Append(' ').Append(command.Category);
        var normalized = Normalize(haystack.ToString());
        var needle = Normalize(query);

        // So khớp chuỗi con; hỗ trợ "gõ tắt" (khoảng trắng bị bỏ qua).
        if (normalized.Contains(needle, StringComparison.OrdinalIgnoreCase)) return true;

        var compactNeedle = needle.Replace(" ", string.Empty);
        if (compactNeedle.Length > 0 &&
            normalized.Replace(" ", string.Empty).Contains(compactNeedle, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Khớp theo chữ cái đầu của các từ (ví dụ "ttf" → "Thêm tệp").
        var initials = string.Concat(
            command.Title.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(w => char.ToLower(w[0], CultureInfo.InvariantCulture)));
        return initials.Contains(compactNeedle, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string value)
    {
        // Bỏ dấu để gõ không dấu vẫn tìm được ("lich" → "lịch").
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace("đ", "d")
            .Replace("Đ", "D")
            .ToLowerInvariant();
    }
}
