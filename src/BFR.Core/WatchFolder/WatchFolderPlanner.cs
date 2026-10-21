namespace BFR.Core.WatchFolder;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Quyết định tệp nào "đã ổn định" để xử lý (FR-D4).
///
/// Tách riêng khỏi <see cref="FileSystemWatcher"/> để kiểm thử được logic
/// thời gian: tệp chỉ được xử lý sau khi không còn sự kiện nào trong khoảng
/// "settle delay" (tức là bản sao chép đã xong).
/// </summary>
public sealed class WatchFolderPlanner
{
    private sealed class Pending
    {
        public DateTime LastEventUtc { get; set; }
        public int Events { get; set; }
    }

    private readonly Dictionary<string, Pending> _pending = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Số tệp đang chờ.</summary>
    public int PendingCount => _pending.Count;

    /// <summary>Ghi nhận một sự kiện của hệ thống tệp cho đường dẫn.</summary>
    public void Notify(string path, DateTime eventTimeUtc)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        if (_pending.TryGetValue(path, out var pending))
        {
            pending.LastEventUtc = eventTimeUtc;
            pending.Events++;
        }
        else
        {
            _pending[path] = new Pending { LastEventUtc = eventTimeUtc, Events = 1 };
        }
    }

    /// <summary>
    /// Lấy ra danh sách tệp đã ổn định (không còn sự kiện trong <paramref name="settleDelay"/>).
    /// </summary>
    public IReadOnlyList<string> TakeReady(DateTime nowUtc, TimeSpan settleDelay)
    {
        var ready = new List<string>();

        foreach (var entry in _pending.ToList())
        {
            if (nowUtc - entry.Value.LastEventUtc < settleDelay) continue;

            ready.Add(entry.Key);
            _pending.Remove(entry.Key);
        }

        return ready;
    }

    /// <summary>Bỏ một đường dẫn khỏi hàng đợi (ví dụ sau khi đã xử lý).</summary>
    public bool Forget(string path) => _pending.Remove(path);

    public void Clear() => _pending.Clear();
}
