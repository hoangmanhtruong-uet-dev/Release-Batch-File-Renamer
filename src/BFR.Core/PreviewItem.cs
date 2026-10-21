namespace BFR.Core;

using System.Collections.Generic;

/// <summary>
/// Kết quả preview của một tệp.
/// </summary>
public sealed record PreviewItem(
    FileItem File,
    string OldName,
    string NewName,
    PreviewStatus Status,
    IReadOnlyList<string> Trace,
    string? Detail = null);

/// <summary>
/// Kết quả của một lượt Preview trên toàn bộ danh sách.
/// </summary>
public sealed record PreviewResult(
    IReadOnlyList<PreviewItem> Items,
    bool HasBlockingError);
