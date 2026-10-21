namespace BFR.Core.Licensing;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Dữ liệu giấy phép lưu cục bộ (KHÔNG chứa thông tin nhạy cảm).
/// Chỉ ghi nhận: đã dùng dùng thử chưa, thời điểm bắt đầu dùng thử, mã sản phẩm đã mua.
/// </summary>
public sealed class LicenseRecord
{
    /// <summary>Product ID của add-on Pro đã mua (nếu có).</summary>
    public string? PurchasedProductId { get; set; }

    /// <summary>Thời điểm mua (UTC).</summary>
    public DateTime? PurchasedUtc { get; set; }

    /// <summary>Thời điểm bắt đầu dùng thử (UTC). Chỉ dùng một lần cho mỗi cài đặt.</summary>
    public DateTime? TrialStartedUtc { get; set; }

    /// <summary>Product ID của gói dùng thử (nếu có).</summary>
    public string? TrialProductId { get; set; }

    /// <summary>Mã kích hoạt đã nhập (bản portable); null nếu kích hoạt qua Store.</summary>
    public string? ActivationKey { get; set; }

    /// <summary>Nguồn kích hoạt: "store" hoặc "key".</summary>
    public string ActivationSource { get; set; } = "store";

    /// <summary>True nếu người dùng đã từng bắt đầu dùng thử (dù đã hết hạn).</summary>
    [JsonIgnore]
    public bool TrialConsumed => TrialStartedUtc.HasValue;
}
