namespace BFR.Core;

using System.Text;

/// <summary>
/// Quy tắc Cắt tỉa (FR-B5).
/// Mọi vị trí tính theo phần tử văn bản (text element) để không cắt ngang ký tự có dấu ghép.
/// </summary>
public sealed class TrimRule : IRule
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public bool Enabled { get; set; } = true;

    public string RemoveChars { get; set; } = string.Empty;      // ví dụ "()[]{}"
    public bool CollapseSpaces { get; set; } = false;
    public bool TrimEnds { get; set; } = false;
    public int RemoveFirstN { get; set; } = 0;
    public int RemoveLastN { get; set; } = 0;
    public int? RemoveRangeStart { get; set; } = null;
    public int? RemoveRangeEnd { get; set; } = null;
    public bool RemoveTrailingDotSpace { get; set; } = false;

    public RenameState Apply(FileItem file, RenameState state, RuleContext ctx)
    {
        var trace = new List<string>(state.Trace) { $"{Id}: Trim" };
        var s = state.Name;

        // (1) xóa ký tự thuộc tập chỉ định
        if (!string.IsNullOrEmpty(RemoveChars))
        {
            var remove = RemoveChars.ToHashSet();
            s = new string(s.Where(ch => !remove.Contains(ch)).ToArray());
        }

        // (2) xóa N ký tự đầu/cuối
        if (RemoveFirstN > 0 && RemoveFirstN < s.Length) s = s[RemoveFirstN..];
        if (RemoveLastN > 0 && RemoveLastN < s.Length) s = s[..^RemoveLastN];

        // (3) xóa theo khoảng vị trí [a, b)
        if (RemoveRangeStart.HasValue && RemoveRangeEnd.HasValue)
        {
            var a = Math.Clamp(RemoveRangeStart.Value, 0, s.Length);
            var b = Math.Clamp(RemoveRangeEnd.Value, a, s.Length);
            s = s[..a] + s[b..];
        }

        // (4) gộp khoảng trắng liên tiếp
        if (CollapseSpaces)
        {
            var sb = new StringBuilder(s.Length);
            bool prevSpace = false;
            foreach (var ch in s)
            {
                if (char.IsWhiteSpace(ch))
                {
                    if (!prevSpace) sb.Append(' ');
                    prevSpace = true;
                }
                else
                {
                    sb.Append(ch);
                    prevSpace = false;
                }
            }
            s = sb.ToString();
        }

        // (5) cắt khoảng trắng đầu/cuối
        if (TrimEnds) s = s.Trim();

        // (6) xóa dấu chấm/khoảng trắng ở cuối tên
        if (RemoveTrailingDotSpace) s = s.TrimEnd('.', ' ');

        return state with { Name = s, Trace = trace };
    }
}
