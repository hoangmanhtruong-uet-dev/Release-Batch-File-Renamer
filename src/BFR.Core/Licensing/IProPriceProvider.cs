namespace BFR.Core.Licensing;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Thông tin giá của add-on Pro.
/// </summary>
public sealed record ProPriceInfo(string? FormattedPrice, string CurrencyCode = "", string Source = "")
{
    public bool HasPrice => !string.IsNullOrWhiteSpace(FormattedPrice);
}

/// <summary>
/// Nguồn lấy giá Pro. Mặc định ứng dụng hỏi Microsoft Store (chỉ hoạt động trong gói MSIX),
/// nếu không có thì dùng giá cấu hình trong cài đặt — không hard-code giá trong mã nguồn.
/// </summary>
public interface IProPriceProvider
{
    /// <summary>True nếu nguồn giá có thể truy vấn được (ví dụ: đã đóng gói MSIX).</summary>
    bool IsAvailable { get; }

    Task<ProPriceInfo> GetPriceAsync(string productId, CancellationToken ct = default);
}
