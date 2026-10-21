namespace BFR.Core;

using System.Globalization;
using System.Text;

/// <summary>
/// Quy tắc Đổi hoa/thường (FR-B7).
/// </summary>
public sealed class CaseRule : IRule
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public bool Enabled { get; set; } = true;

    public CaseMode Mode { get; set; } = CaseMode.Lower;
    public ApplyScope Scope { get; set; } = ApplyScope.Name;
    public HashSet<string> Exceptions { get; } = new(StringComparer.OrdinalIgnoreCase);

    public RenameState Apply(FileItem file, RenameState state, RuleContext ctx)
    {
        var trace = new List<string>(state.Trace) { $"{Id}: Case" };

        var name = Scope is ApplyScope.Name or ApplyScope.Both ? ApplyMode(state.Name, Mode) : state.Name;
        var ext = Scope is ApplyScope.Extension or ApplyScope.Both ? ApplyMode(state.Extension, Mode) : state.Extension;

        return state with { Name = name, Extension = ext, Trace = trace };
    }

    private string ApplyMode(string s, CaseMode mode)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return mode switch
        {
            CaseMode.Lower => s.ToLowerInvariant(),
            CaseMode.Upper => s.ToUpperInvariant(),
            CaseMode.Title => ToTitleCase(s),
            CaseMode.Sentence => ToSentenceCase(s),
            _ => s
        };
    }

    private string ToTitleCase(string s)
    {
        var words = s.Split(' ');
        var sb = new StringBuilder(s.Length);
        for (int i = 0; i < words.Length; i++)
        {
            if (i > 0) sb.Append(' ');
            var w = words[i];
            if (w.Length == 0) continue;
            if (Exceptions.Contains(w)) { sb.Append(w); continue; }
            // Tôn trọng camelCase / iPhone nếu từ có chữ hoa ở giữa
            if (w.Any(char.IsUpper) && w.Skip(1).Any(char.IsUpper)) { sb.Append(w); continue; }
            sb.Append(char.ToUpperInvariant(w[0]));
            if (w.Length > 1) sb.Append(w[1..].ToLowerInvariant());
        }
        return sb.ToString();
    }

    private static string ToSentenceCase(string s)
    {
        if (s.Length == 0) return s;
        var first = char.ToUpperInvariant(s[0]);
        return first + s[1..].ToLowerInvariant();
    }

    public enum CaseMode { Lower, Upper, Title, Sentence }
    public enum ApplyScope { Name, Extension, Both }
}
