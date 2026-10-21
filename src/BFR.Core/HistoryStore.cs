namespace BFR.Core;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

/// <summary>
/// Lịch sử các lần áp dụng (PHẦN 7.6, 8.5) — lưu nguyên tử, giữ 30 ngày (Pro).
/// </summary>
public sealed class HistoryEntry
{
    public string Id { get; set; } = string.Empty;
    public DateTime StartedUtc { get; set; }
    public string? Preset { get; set; }
    public int Total { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public string Status { get; set; } = "completed";
    public string JournalPath { get; set; } = string.Empty;
}

public sealed class HistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly IFileSystem _fs;
    private readonly string _path;

    public HistoryStore(IFileSystem fs, string? path = null)
    {
        _fs = fs;
        _path = path ?? Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "BatchFileRenamer", "history.json");
    }

    public IReadOnlyList<HistoryEntry> LoadAll()
    {
        if (!_fs.FileExists(_path)) return Array.Empty<HistoryEntry>();
        try
        {
            var json = _fs.ReadAllText(_path);
            return JsonSerializer.Deserialize<List<HistoryEntry>>(json, JsonOptions) ?? new List<HistoryEntry>();
        }
        catch (JsonException)
        {
            return Array.Empty<HistoryEntry>();
        }
    }

    public void Add(HistoryEntry entry)
    {
        var list = LoadAll().ToList();
        list.Insert(0, entry);
        Save(list);
    }

    public void Save(IEnumerable<HistoryEntry> entries)
    {
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir)) _fs.EnsureDirectory(dir);
        var temp = _path + ".tmp";
        _fs.WriteAllText(temp, JsonSerializer.Serialize(entries, JsonOptions));
        _fs.Move(temp, _path, overwrite: true);
    }

    public void Prune(TimeSpan maxAge)
    {
        var cutoff = DateTime.UtcNow - maxAge;
        var kept = LoadAll().Where(e => e.StartedUtc >= cutoff).ToList();
        Save(kept);
    }

    /// <summary>
    /// Giới hạn số mục lịch sử được giữ lại (Free: 1 lần gần nhất; Pro: không giới hạn).
    /// </summary>
    public int Limit(int maxEntries)
    {
        if (maxEntries == int.MaxValue) return LoadAll().Count;

        var kept = LoadAll().Take(maxEntries).ToList();
        Save(kept);
        return kept.Count;
    }

    /// <summary>
    /// Dọn lịch sử theo thời gian giữ (Pro: 30 ngày) và số mục tối đa (Free: 1).
    /// </summary>
    public int PruneAndLimit(TimeSpan maxAge, int maxEntries, DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        var cutoff = now - maxAge;
        var kept = LoadAll()
            .Where(e => e.StartedUtc >= cutoff)
            .Take(maxEntries == int.MaxValue ? int.MaxValue : maxEntries)
            .ToList();
        Save(kept);
        return kept.Count;
    }
}
