namespace BFR.Core;

using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Tự xử lý trùng tên (B3): khi hai tệp cùng thư mục có tên mới giống nhau, hoặc tên mới trùng
/// tệp/thư mục ngoài lô, thêm hậu tố " (2)", " (3)"… vào TRƯỚC phần mở rộng cho tới khi duy nhất.
///
/// Logic thuần, không đọc đĩa: dùng cùng dữ liệu "tên ngoài lô" mà <see cref="ConflictDetector"/> nhận.
/// Tên không hợp lệ (ký tự cấm, tên dành riêng...) không được sửa ở đây — chúng vẫn bị chặn như cũ.
/// </summary>
public static class ConflictResolver
{
    public sealed record Result(IReadOnlyList<PreviewItem> Items, int ResolvedCount);

    /// <param name="items">Kết quả thô của chuỗi quy tắc (trước khi phát hiện xung đột).</param>
    /// <param name="outsideFor">Tên đang tồn tại ngoài lô theo từng tệp (có thể trả về null).</param>
    public static Result Resolve(
        IReadOnlyList<PreviewItem> items,
        Func<PreviewItem, IReadOnlySet<string>?> outsideFor)
    {
        var validator = new NameValidator();

        // Tên đã bị chiếm, theo từng thư mục (không phân biệt hoa/thường như Windows).
        var taken = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        HashSet<string> TakenFor(PreviewItem it)
        {
            var key = ConflictDetector.NormalizeDirectory(it.File.Directory);
            if (!taken.TryGetValue(key, out var set))
            {
                set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                taken[key] = set;
            }
            return set;
        }

        // Bước 1: tệp giữ nguyên tên giữ chỗ trước nhất; tệp tên không hợp lệ cũng giữ nguyên chỗ của nó.
        foreach (var it in items)
        {
            var unchanged = string.Equals(it.NewName, it.OldName, StringComparison.Ordinal);
            if (unchanged || validator.Validate(it.NewName) != NameIssue.None)
            {
                TakenFor(it).Add(it.NewName);
            }
        }

        var result = new List<PreviewItem>(items.Count);
        var resolved = 0;

        // Bước 2: các tệp đổi tên, theo thứ tự trong lô.
        foreach (var it in items)
        {
            var unchanged = string.Equals(it.NewName, it.OldName, StringComparison.Ordinal);
            if (unchanged || validator.Validate(it.NewName) != NameIssue.None)
            {
                result.Add(it);
                continue;
            }

            var set = TakenFor(it);
            var outside = outsideFor(it);

            bool IsFree(string name) => !set.Contains(name) && (outside is null || !outside.Contains(name));

            var final = it.NewName;
            if (!IsFree(final))
            {
                var stem = Path.GetFileNameWithoutExtension(final);
                var ext = Path.GetExtension(final);
                for (var n = 2; n < 100_000; n++)
                {
                    var candidate = $"{stem} ({n}){ext}";
                    if (IsFree(candidate)) { final = candidate; break; }
                }
            }

            set.Add(final);
            if (!string.Equals(final, it.NewName, StringComparison.Ordinal))
            {
                resolved++;
                var trace = new List<string>(it.Trace) { $"Tự xử lý trùng tên → {final}" };
                result.Add(it with { NewName = final, Trace = trace });
            }
            else
            {
                result.Add(it);
            }
        }

        return new Result(result, resolved);
    }
}
