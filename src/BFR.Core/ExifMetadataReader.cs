namespace BFR.Core;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

/// <summary>
/// Đọc ngày chụp EXIF (DateTimeOriginal → DateTimeDigitized → IFD0 DateTime).
/// Có bộ nhớ đệm theo (đường dẫn, thời gian sửa, kích thước).
/// </summary>
public sealed class ExifMetadataReader : IMetadataProvider
{
    private sealed record CacheEntry(IReadOnlyDictionary<string, string> Map);

    private readonly BoundedCache<string, CacheEntry> _cache = new(capacity: 8192);

    public IReadOnlyDictionary<string, string> Read(FileItem file)
    {
        if (file.IsDirectory || file.Size == 0) return Empty;

        // Tệp placeholder OneDrive: KHÔNG đọc nội dung để tránh tải tệp về máy
        // ngoài ý muốn của người dùng (edge case #14, QA-14).
        if (file.IsOffline) return Empty;

        // Bỏ qua tệp chắc chắn không có EXIF để không mở tệp vô ích (NFR-1).
        if (!MediaFileTypes.MayHaveExif(file.Extension)) return Empty;

        var key = $"{file.FullPath}|{file.ModifiedUtc.Ticks}|{file.Size}";
        if (_cache.TryGet(key, out var cached)) return cached.Map;

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var directories = ImageMetadataReader.ReadMetadata(file.FullPath);
            var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();

            DateTime? taken = null;
            if (subIfd is not null && subIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var dtOriginal))
                taken = dtOriginal;
            else if (subIfd is not null && subIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeDigitized, out var dtDigitized))
                taken = dtDigitized;
            else if (ifd0 is not null && ifd0.TryGetDateTime(ExifDirectoryBase.TagDateTime, out var dtIfd0))
                taken = dtIfd0;

            if (taken.HasValue && taken.Value.Year > 1900)
                map["taken"] = DateTime.SpecifyKind(taken.Value, DateTimeKind.Local).ToString("O");
        }
        catch (Exception)
        {
            // Tệp không phải ảnh, hỏng, hoặc OneDrive placeholder không truy cập được.
        }

        var entry = new CacheEntry(map);
        _cache.Set(key, entry);
        return map;
    }

    private static readonly IReadOnlyDictionary<string, string> Empty =
        new Dictionary<string, string>();
}
