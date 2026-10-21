namespace BFR.Core.Licensing;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Ảnh chụp giấy phép từ Microsoft Store.
/// </summary>
public sealed record StoreLicenseSnapshot
{
    /// <summary>Add-on Pro đã mua và còn hiệu lực.</summary>
    public bool IsProActive { get; init; }

    /// <summary>Đang trong thời gian gia hạn (grace period) sau khi hủy đăng ký.</summary>
    public bool InGracePeriod { get; init; }

    /// <summary>Ngày hết hạn của giấy phép (null nếu mua đứt trọn đời).</summary>
    public DateTimeOffset? ExpirationDate { get; init; }

    /// <summary>Giấy phép dùng thử của Store còn hiệu lực.</summary>
    public bool IsTrialActive { get; init; }

    /// <summary>Ngày hết hạn dùng thử của Store.</summary>
    public DateTimeOffset? TrialExpirationDate { get; init; }

    /// <summary>Thông điệp lỗi (nếu truy vấn thất bại).</summary>
    public string? Error { get; init; }

    public bool HasPro => IsProActive || InGracePeriod;
}

/// <summary>Kết quả một giao dịch mua trong Store.</summary>
public enum PurchaseStatus
{
    Completed,
    NotPurchased,
    UserCancelled,
    StoreNotAvailable,
    NotSupported,
    Failed
}

public sealed record PurchaseOutcome(PurchaseStatus Status, string? Error = null)
{
    public bool IsSuccess => Status == PurchaseStatus.Completed;
}

/// <summary>
/// Trừu tượng hoá Microsoft Store IAP để tầng lõi không phụ thuộc Windows Runtime.
/// Bản portable (không có package identity) trả về "không khả dụng".
/// </summary>
public interface IStoreLicenseProvider
{
    /// <summary>True nếu môi trường hiện tại có thể dùng Store IAP (ứng dụng đã đóng gói MSIX).</summary>
    bool IsAvailable { get; }

    /// <summary>Truy vấn trạng thái giấy phép hiện tại.</summary>
    Task<StoreLicenseSnapshot> GetLicenseAsync(CancellationToken ct = default);

    /// <summary>Mua add-on Pro.</summary>
    Task<PurchaseOutcome> PurchaseAsync(string productId, CancellationToken ct = default);
}
