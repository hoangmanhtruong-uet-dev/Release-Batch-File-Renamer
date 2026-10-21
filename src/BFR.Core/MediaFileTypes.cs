namespace BFR.Core;

using System;
using System.Collections.Generic;

/// <summary>
/// Danh sách phần mở rộng có metadata ngày chụp / ngày tạo.
///
/// Dùng để **bỏ qua việc mở tệp** khi tệp chắc chắn không có metadata
/// (ví dụ .txt, .pdf, .zip): giúp preview EXIF trên lô 100.000 tệp không phải
/// mở hàng trăm nghìn tệp không liên quan (NFR-1).
/// </summary>
public static class MediaFileTypes
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".jpe", ".png", ".heic", ".heif", ".tif", ".tiff", ".webp",
        ".gif", ".bmp", ".dng", ".raw", ".cr2", ".cr3", ".nef", ".arw", ".orf", ".rw2",
        ".raf", ".srw", ".psd"
    };

    private static readonly HashSet<string> MediaExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".m4v", ".mov", ".avi", ".mkv", ".wmv", ".flv", ".webm", ".3gp", ".m2ts",
        ".mp3", ".m4a", ".wav", ".aac", ".flac", ".ogg", ".wma", ".opus", ".aiff"
    };

    /// <summary>True nếu phần mở rộng có thể chứa EXIF (ngày chụp).</summary>
    public static bool MayHaveExif(string? extension) => ImageExtensions.Contains(Normalize(extension));

    /// <summary>True nếu phần mở rộng có thể chứa ngày tạo media.</summary>
    public static bool MayHaveMediaDate(string? extension) => MediaExtensions.Contains(Normalize(extension));

    /// <summary>True nếu tệp có thể chứa metadata ngày nào đó.</summary>
    public static bool MayHaveAnyMetadata(string? extension) =>
        MayHaveExif(extension) || MayHaveMediaDate(extension);

    private static string Normalize(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension)) return string.Empty;
        var ext = extension.Trim();
        return ext.StartsWith('.') ? ext : "." + ext;
    }
}
