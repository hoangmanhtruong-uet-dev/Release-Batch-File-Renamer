namespace BFR.Core;

using System.Collections.Generic;

/// <summary>
/// Biến danh sách (cũ → mới) thành kế hoạch thực thi có thứ tự.
/// Xử lý chuỗi phụ thuộc và vòng hoán đổi bằng tên tạm (PHẦN 7.3).
/// </summary>
public enum StepKind { Direct, ToTemp, FromTemp }

public sealed record Step(string From, string To, StepKind Kind);

public sealed class RenamePlanner
{
    /// <summary>
    /// pairs: danh sách (From, To) đã hợp lệ và không xung đột loại 1-2.
    /// Trả về danh sách bước có thứ tự an toàn.
    /// </summary>
    public IReadOnlyList<Step> Plan(IReadOnlyList<(string From, string To)> pairs)
    {
        var byFrom = pairs.ToDictionary(p => p.From, StringComparer.OrdinalIgnoreCase);
        var steps = new List<Step>();
        var pendingFromTemp = new List<(string Temp, string To)>();
        var state = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); // 0=chưa, 1=đang, 2=xong

        void Visit((string From, string To) p)
        {
            if (state.TryGetValue(p.From, out var s) && s == 2) return;

            if (state.TryGetValue(p.From, out s) && s == 1)
            {
                // phát hiện vòng: đưa From về tên tạm trước
                var temp = MakeTempName(p.From);
                steps.Add(new Step(p.From, temp, StepKind.ToTemp));
                pendingFromTemp.Add((temp, p.To));
                state[p.From] = 2;
                return;
            }

            state[p.From] = 1;

            // Nếu tên đích đang bị một tệp khác (cũng đang đổi) chiếm, xử lý tệp đó trước
            if (byFrom.TryGetValue(p.To, out var blocker) && blocker.From != p.From)
            {
                Visit(blocker);
            }

            if (!state.TryGetValue(p.From, out var s2) || s2 != 2)
            {
                steps.Add(new Step(p.From, p.To, StepKind.Direct));
                state[p.From] = 2;
            }
        }

        foreach (var p in pairs) Visit(p);

        foreach (var (temp, to) in pendingFromTemp)
        {
            steps.Add(new Step(temp, to, StepKind.FromTemp));
        }

        return steps.AsReadOnly();
    }

    private static string MakeTempName(string from)
    {
        var ext = System.IO.Path.GetExtension(from);
        var stem = System.IO.Path.GetFileNameWithoutExtension(from);
        return $"{stem}.bfr-{Guid.NewGuid():N}.tmp{ext}";
    }
}
