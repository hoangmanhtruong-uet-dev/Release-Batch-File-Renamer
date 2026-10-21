namespace BFR.Core;

using System.Text;

/// <summary>
/// Quy tắc Số thứ tự (FR-B3).
/// Giá trị số của tệp thứ i trong nhóm: value = start + step * GroupIndex.
/// </summary>
public sealed class NumberingRule : IRule
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public bool Enabled { get; set; } = true;

    public int Start { get; set; } = 1;
    public int Step { get; set; } = 1;
    public int? Width { get; set; } = null; // null = tự động
    public NumberPosition Position { get; set; } = NumberPosition.Start;
    public string Separator { get; set; } = "_";
    public NumberResetMode ResetMode { get; set; } = NumberResetMode.None;
    public string? ResetToken { get; set; } = null;
    public NumberKind Kind { get; set; } = NumberKind.Decimal;

    public RenameState Apply(FileItem file, RenameState state, RuleContext ctx)
    {
        var trace = new List<string>(state.Trace) { $"{Id}: Numbering" };

        int value = Start + Step * state.GroupIndex;
        string num = Kind switch
        {
            NumberKind.Decimal => FormatDecimal(value),
            NumberKind.Letters => ToLetters(value),
            NumberKind.Roman => ToRoman(value),
            _ => FormatDecimal(value)
        };

        string result = Position switch
        {
            NumberPosition.Start => $"{num}{Separator}{state.Name}",
            NumberPosition.End => $"{state.Name}{Separator}{num}",
            NumberPosition.ReplaceAll => num,
            _ => InsertAt(state.Name, num, 0) // Index position không cấu hình trong prototype; đặt đầu tên
        };

        return state with { Name = result, Trace = trace };

        string FormatDecimal(int v)
        {
            var s = v.ToString();
            var w = Width ?? AutoWidth(file.LoadOrder + 1); // tổng số tệp xấp xỉ
            return s.PadLeft(w, '0');
        }

        static int AutoWidth(int total) => total switch
        {
            < 10 => 1,
            < 100 => 2,
            < 1000 => 3,
            < 10000 => 4,
            _ => 5
        };
    }

    static string InsertAt(string name, string num, int index)
    {
        if (index <= 0) return $"{num}{name}";
        if (index >= name.Length) return $"{name}{num}";
        return name.Insert(index, num);
    }

    // --- Helpers for Letter / Roman kinds (kept simple for prototype) ---
    static string ToLetters(int v)
    {
        var sb = new StringBuilder();
        while (v > 0)
        {
            v--;
            sb.Insert(0, (char)('A' + (v % 26)));
            v /= 26;
        }
        return sb.ToString();
    }

    static string ToRoman(int v)
    {
        var pairs = new[] { (1000, "M"), (900, "CM"), (500, "D"), (400, "CD"), (100, "C"), (90, "XC"), (50, "L"), (40, "XL"), (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I") };
        var sb = new StringBuilder();
        foreach (var (val, sym) in pairs)
        {
            while (v >= val) { sb.Append(sym); v -= val; }
        }
        return sb.ToString();
    }

    public enum NumberPosition { Start, End, ReplaceAll, Index }
    public enum NumberResetMode { None, ByFolder, ByTokenChange }
    public enum NumberKind { Decimal, Letters, Roman }
}
