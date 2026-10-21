namespace BFR.Core.WatchFolder;

using System;

/// <summary>
/// Tuỳ chọn cho Watch Folder (FR-D4, tính năng Pro của v1.2).
/// </summary>
public sealed class WatchFolderOptions
{
    /// <summary>Thư mục cần theo dõi.</summary>
    public string Folder { get; set; } = string.Empty;

    /// <summary>Tên preset trong thư viện dùng để đổi tên tự động.</summary>
    public string PresetName { get; set; } = string.Empty;

    /// <summary>Theo dõi cả thư mục con.</summary>
    public bool Recursive { get; set; } = true;

    /// <summary>
    /// Thời gian chờ tệp "ổn định" (không còn thay đổi kích thước/thời gian sửa)
    /// trước khi áp dụng, để không đổi tên tệp đang được sao chép.
    /// </summary>
    public int SettleDelayMs { get; set; } = 1500;

    /// <summary>Chu kỳ gom lô kiểm tra tệp sẵn sàng.</summary>
    public int DebounceMs { get; set; } = 500;

    /// <summary>Số tệp tối đa xử lý trong một lô.</summary>
    public int MaxFilesPerBatch { get; set; } = 500;

    /// <summary>Chỉ xử lý tệp khớp mẫu (ví dụ *.jpg). Rỗng = mọi tệp.</summary>
    public string? IncludeGlob { get; set; }

    /// <summary>Bỏ qua tệp khớp mẫu.</summary>
    public string? ExcludeGlob { get; set; }

    /// <summary>Chỉ tính toán, không ghi gì (chế độ an toàn cho script).</summary>
    public bool DryRun { get; set; }

    public TimeSpan SettleDelay => TimeSpan.FromMilliseconds(Math.Max(0, SettleDelayMs));

    public TimeSpan Debounce => TimeSpan.FromMilliseconds(Math.Max(100, DebounceMs));
}
