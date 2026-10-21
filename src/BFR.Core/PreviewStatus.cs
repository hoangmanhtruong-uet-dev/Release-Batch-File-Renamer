namespace BFR.Core;

/// <summary>
/// Trạng thái từng dòng preview (FR-C2).
/// </summary>
public enum PreviewStatus
{
    Ok,
    Unchanged,
    Conflict,
    InvalidName,
    PathTooLong,
    Locked,
    MissingData
}
