namespace BFR.Core.Licensing;

/// <summary>
/// Kết quả kiểm tra một tính năng có bị khóa hay không.
/// </summary>
public sealed record GateResult(bool Allowed, string ReasonKey, string Message)
{
    public static GateResult Allow() => new(true, string.Empty, string.Empty);

    public static GateResult Deny(string reasonKey, string message) => new(false, reasonKey, message);

    /// <summary>Thông điệp chuẩn hoá để giao diện hiển thị (tiếng Việt/Anh do tầng trên quyết định).</summary>
    public string Describe() => string.IsNullOrEmpty(ReasonKey) ? string.Empty : ReasonKey;
}
