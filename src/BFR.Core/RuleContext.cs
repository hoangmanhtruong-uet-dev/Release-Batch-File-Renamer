namespace BFR.Core;

using System.Collections.Generic;

/// <summary>
/// Ngữ cảnh được đóng băng một lần cho mỗi lượt Preview.
/// Mọi dữ liệu ngoài (ngày hiện tại, hạt giống ngẫu nhiên, metadata EXIF...)
/// được truyền qua đối tượng này để Preview và Áp dụng cho ra kết quả giống hệt nhau.
/// </summary>
public sealed record RuleContext(
    DateTime Now,
    string RandomSeed,
    IReadOnlyDictionary<string, string>? Metadata = null,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>? FileMetadata = null)
{
    /// <summary>
    /// Metadata của đúng tệp đang xử lý; nếu không có bản theo tệp thì dùng Metadata chung.
    /// </summary>
    public IReadOnlyDictionary<string, string>? MetadataFor(FileItem file)
    {
        if (FileMetadata is not null && FileMetadata.TryGetValue(file.FullPath, out var perFile))
            return perFile;
        return Metadata;
    }
}
