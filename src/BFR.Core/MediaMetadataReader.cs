namespace BFR.Core;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MetadataExtractor;

/// <summary>
/// Đọc ngày tạo của tệp media (video/audio) — tính năng Pro (FR-B4).
///
/// Dùng MetadataExtractor và quét các thẻ có nhãn chứa "creation date"/"creation time"/
/// "media create date" nên hoạt động với MP4/MOV/AVI và các container phổ biến.
///
/// An toàn: KHÔNG đọc tệp đang ở trạng thái "chỉ trực tuyến" (OneDrive placeholder)
/// để không vô tình tải tệp về máy (edge case #14, QA-14).
/// </summary>
public sealed class MediaMetadataReader : IMetadataProvider
{
    private sealed record CacheEntry(IReadOnlyDictionary<string, string> Map);

    private static readonly string[] CreationDateLabels =
    {
        "creation date",
        "media create date",
        "media creation date",
        "creation time",
        "date created",
        "recorded date",
        "encoding time"
    };

    private readonly BoundedCache<string, CacheEntry> _cache = new(capacity: 8192);

    public IReadOnlyDictionary<string, string> Read(FileItem file)
    {
        if (file.IsDirectory || file.Size == 0) return Empty;

        // Tệp placeholder OneDrive: không đọc để tránh tải tệp về ngoài ý muốn.
        if (file.IsOffline) return Empty;

        // Bỏ qua tệp chắc chắn không phải media để không mở tệp vô ích (NFR-1).
        if (!MediaFileTypes.MayHaveMediaDate(file.Extension)) return Empty;

        var key = $"{file.FullPath}|{file.ModifiedUtc.Ticks}|{file.Size}";
        if (_cache.TryGet(key, out var cached)) return cached.Map;

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var directories = ImageMetadataReader.ReadMetadata(file.FullPath);
            foreach (var directory in directories)
            {
                foreach (var tag in directory.Tags)
                {
                    var label = tag.Name ?? string.Empty;
                    if (!CreationDateLabels.Any(l => label.Contains(l, StringComparison.OrdinalIgnoreCase))) continue;

                    if (!directory.TryGetDateTime(tag.Type, out var value)) continue;
                    if (value.Year < 1900 || value.Year > 2200) continue;

                    map["media"] = DateTime.SpecifyKind(value, DateTimeKind.Local).ToString("O");
                    break;
                }

                if (map.Count > 0) break;
            }
        }
        catch (Exception)
        {
            // Tệp không phải media, hỏng, hoặc không truy cập được: bỏ qua.
        }

        var entry = new CacheEntry(map);
        _cache.Set(key, entry);
        return entry.Map;
    }

    private static readonly IReadOnlyDictionary<string, string> Empty =
        new Dictionary<string, string>();
}
