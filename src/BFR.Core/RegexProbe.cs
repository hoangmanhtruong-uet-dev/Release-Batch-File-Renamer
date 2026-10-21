namespace BFR.Core;

using System.Text.RegularExpressions;

/// <summary>Kết quả khớp của một Regex trên chuỗi đầu vào.</summary>
public sealed class RegexProbeMatch
{
    public RegexProbeMatch(int index, int length, string value, IReadOnlyList<string> groups)
    {
        Index = index;
        Length = length;
        Value = value;
        Groups = groups;
    }

    /// <summary>Vị trí bắt đầu trong chuỗi gốc.</summary>
    public int Index { get; }

    /// <summary>Độ dài đoạn khớp.</summary>
    public int Length { get; }

    /// <summary>Đoạn khớp.</summary>
    public string Value { get; }

    /// <summary>Các nhóm bắt (nhóm 0 là cả đoạn khớp).</summary>
    public IReadOnlyList<string> Groups { get; }
}

/// <summary>Kết quả thử Regex: có khớp hay không, chuỗi sau khi thay, và thông báo lỗi.</summary>
public sealed class RegexProbeResult
{
    private RegexProbeResult(bool isValid, string? error, string output, IReadOnlyList<RegexProbeMatch> matches)
    {
        IsValid = isValid;
        Error = error;
        Output = output;
        Matches = matches;
    }

    /// <summary>True nếu mẫu biểu thức hợp lệ (không có lỗi cú pháp).</summary>
    public bool IsValid { get; }

    /// <summary>Thông báo lỗi (rỗng nếu hợp lệ).</summary>
    public string? Error { get; }

    /// <summary>Chuỗi kết quả sau khi thay thế.</summary>
    public string Output { get; }

    /// <summary>Danh sách các đoạn khớp.</summary>
    public IReadOnlyList<RegexProbeMatch> Matches { get; }

    /// <summary>True nếu có ít nhất một đoạn khớp.</summary>
    public bool HasMatch => Matches.Count > 0;

    internal static RegexProbeResult Valid(string output, IReadOnlyList<RegexProbeMatch> matches) =>
        new(true, null, output, matches);

    internal static RegexProbeResult Invalid(string error) =>
        new(false, error, string.Empty, Array.Empty<RegexProbeMatch>());
}

/// <summary>
/// Đo Regex để dùng cho "Trình thử Regex". Tách riêng khỏi UI để unit test được.
/// Có giới hạn thời gian để tránh treo máy với biểu thức lỗi (catastrophic backtracking).
/// </summary>
public static class RegexProbe
{
    /// <summary>Thời gian tối đa cho một lần thử (mili giây).</summary>
    public const int TimeoutMilliseconds = 2000;

    /// <summary>
    /// Thử một mẫu Regex trên chuỗi đầu vào.
    /// </summary>
    /// <param name="pattern">Mẫu biểu thức.</param>
    /// <param name="replacement">Chuỗi thay thế (hỗ trợ $1, ${name}).</param>
    /// <param name="input">Chuỗi đầu vào.</param>
    /// <param name="ignoreCase">Bỏ qua phân biệt hoa/thường.</param>
    public static RegexProbeResult Evaluate(
        string pattern,
        string replacement,
        string input,
        bool ignoreCase = false)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return RegexProbeResult.Invalid(string.Empty);
        }

        var options = RegexOptions.CultureInvariant;
        if (ignoreCase) options |= RegexOptions.IgnoreCase;

        Regex regex;
        try
        {
            regex = new Regex(pattern, options, TimeSpan.FromMilliseconds(TimeoutMilliseconds));
        }
        catch (ArgumentException ex)
        {
            return RegexProbeResult.Invalid(ex.Message);
        }

        try
        {
            var matches = new List<RegexProbeMatch>();
            foreach (Match match in regex.Matches(input))
            {
                var groups = new List<string>(match.Groups.Count);
                for (int i = 0; i < match.Groups.Count; i++)
                {
                    groups.Add(match.Groups[i].Value);
                }

                matches.Add(new RegexProbeMatch(match.Index, match.Length, match.Value, groups));
            }

            var output = regex.Replace(input, replacement ?? string.Empty);
            return RegexProbeResult.Valid(output, matches);
        }
        catch (RegexMatchTimeoutException)
        {
            return RegexProbeResult.Invalid($"Timeout sau {TimeoutMilliseconds} ms — biểu thức có thể quá phức tạp.");
        }
    }
}
