namespace BFR.Core;

/// <summary>
/// Kiểm tra tên mới hợp lệ (PHẦN 7.1).
/// </summary>
public enum NameIssue
{
    None,
    Empty,
    InvalidChar,
    TrailingDotSpace,
    ReservedName,
    NameTooLong,
    PathTooLong
}

public sealed class NameValidator
{
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON","PRN","AUX","NUL",
        "COM1","COM2","COM3","COM4","COM5","COM6","COM7","COM8","COM9",
        "LPT1","LPT2","LPT3","LPT4","LPT5","LPT6","LPT7","LPT8","LPT9"
    };

    public NameIssue Validate(string fullName, int maxPath = 260)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return NameIssue.Empty;
        if (fullName.Length > 255) return NameIssue.NameTooLong;
        foreach (var ch in fullName)
        {
            if (ch < 32 || "<>:\"/\\|?*".Contains(ch)) return NameIssue.InvalidChar;
        }
        if (fullName.EndsWith('.') || fullName.EndsWith(' ')) return NameIssue.TrailingDotSpace;
        var stem = fullName.Split('.')[0];
        if (Reserved.Contains(stem.TrimEnd(' '))) return NameIssue.ReservedName;
        if (fullName.Length > maxPath) return NameIssue.PathTooLong;
        return NameIssue.None;
    }
}
