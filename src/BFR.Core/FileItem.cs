namespace BFR.Core;

/// <summary>
/// Bản chụp bất biến của một tệp/thư mục tại thời điểm nạp.
/// </summary>
public sealed record FileItem(
    string FullPath,      // đường dẫn đầy đủ hiện tại
    string Directory,     // thư mục chứa
    string Name,          // tên không gồm phần mở rộng
    string Extension,     // phần mở rộng không gồm dấu chấm; "" nếu không có
    bool IsDirectory,
    long Size,
    DateTime CreatedUtc,
    DateTime ModifiedUtc,
    int LoadOrder)        // thứ tự nạp, dùng khi sắp xếp "theo thứ tự nạp"
{
    /// <summary>
    /// True nếu tệp đang ở trạng thái "chỉ trực tuyến" (OneDrive placeholder, offline).
    /// Ứng dụng KHÔNG đọc nội dung/metadata của các tệp này để tránh vô tình tải về máy.
    /// </summary>
    public bool IsOffline { get; init; }

    /// <summary>True nếu tệp được đánh dấu chỉ đọc.</summary>
    public bool IsReadOnly { get; init; }
}
