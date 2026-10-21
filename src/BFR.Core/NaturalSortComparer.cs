namespace BFR.Core;

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// So sánh tự nhiên giống Explorer (StrCmpLogicalW trên Windows, fallback trên nền tảng khác).
/// </summary>
public sealed class NaturalSortComparer : IComparer<string>
{
    public static readonly NaturalSortComparer Instance = new();

    public int Compare(string? x, string? y)
    {
        if (OperatingSystem.IsWindows())
        {
            try { return StrCmpLogicalW(x ?? string.Empty, y ?? string.Empty); }
            catch { /* fallback */ }
        }
        return NaturalCompare(x ?? string.Empty, y ?? string.Empty);
    }

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int StrCmpLogicalW(string x, string y);

    private static int NaturalCompare(string a, string b)
    {
        int i = 0, j = 0;
        while (i < a.Length && j < b.Length)
        {
            if (char.IsDigit(a[i]) && char.IsDigit(b[j]))
            {
                var startA = i; while (i < a.Length && char.IsDigit(a[i])) i++;
                var startB = j; while (j < b.Length && char.IsDigit(b[j])) j++;
                var numA = a[startA..i].TrimStart('0');
                var numB = b[startB..j].TrimStart('0');
                if (numA.Length != numB.Length) return numA.Length.CompareTo(numB.Length);
                var cmp = string.CompareOrdinal(numA, numB);
                if (cmp != 0) return cmp;
            }
            else
            {
                var cmp = char.ToLowerInvariant(a[i]).CompareTo(char.ToLowerInvariant(b[j]));
                if (cmp != 0) return cmp;
                i++; j++;
            }
        }
        return (a.Length - i).CompareTo(b.Length - j);
    }
}
