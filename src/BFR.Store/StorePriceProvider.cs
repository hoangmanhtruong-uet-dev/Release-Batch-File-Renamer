namespace BFR.Store;

using BFR.Core.Licensing;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Services.Store;

/// <summary>
/// Lấy giá add-on Pro từ Microsoft Store (chỉ hoạt động khi ứng dụng đã đóng gói MSIX
/// và add-on đã được cấu hình trong Partner Center).
///
/// Nếu không lấy được (bản portable, ngoại tuyến, add-on chưa cấu hình) → trả về
/// giá rỗng để giao diện dùng giá cấu hình trong Cài đặt.
/// </summary>
public sealed class StorePriceProvider : IProPriceProvider
{
    private StoreContext? _context;

    public bool IsAvailable
    {
        get
        {
            if (_context is not null) return true;

            try
            {
                _context = StoreContext.GetDefault();
                return _context is not null;
            }
            catch (Exception)
            {
                // 0x80073D54: ứng dụng không có package identity.
                _context = null;
                return false;
            }
        }
    }

    public async Task<ProPriceInfo> GetPriceAsync(string productId, CancellationToken ct = default)
    {
        if (!IsAvailable || _context is null)
        {
            return new ProPriceInfo(null, string.Empty, "unavailable");
        }

        try
        {
            // Trong chiếu WinRT, StoreProductKind được chiếu thành chuỗi.
            var products = await _context
                .GetAssociatedStoreProductsAsync(new[] { "DurableAddOn" })
                .AsTask(ct)
                .ConfigureAwait(false);

            // Products là tập KeyValuePair<productId, StoreProduct>.
            var product = products.Products
                .Where(entry => string.Equals(entry.Key, productId, StringComparison.OrdinalIgnoreCase))
                .Select(entry => entry.Value)
                .FirstOrDefault();

            var price = product?.Price;
            return new ProPriceInfo(price?.FormattedPrice, price?.CurrencyCode ?? string.Empty, "store");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return new ProPriceInfo(null, string.Empty, "error");
        }
    }
}
