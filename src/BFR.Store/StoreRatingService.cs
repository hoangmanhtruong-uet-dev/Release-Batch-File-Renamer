namespace BFR.Store;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Services.Store;

[ComImport]
[Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IInitializeWithWindow
{
    void Initialize(IntPtr hwnd);
}

/// <summary>
/// Dịch vụ gọi In-App Rating & Review của Microsoft Store (v1.0.2).
/// Hỗ trợ cả gói cài MSIX và mở fallback trình duyệt nếu chạy bản portable.
/// </summary>
public static class StoreRatingService
{
    public static async Task<bool> RequestRateAndReviewAsync(IntPtr hwnd = default)
    {
        try
        {
            var context = StoreContext.GetDefault();
            if (context is null)
            {
                OpenStoreWebReview();
                return false;
            }

            if (hwnd != IntPtr.Zero)
            {
                var init = (IInitializeWithWindow)(object)context;
                init.Initialize(hwnd);
            }

            var result = await context.RequestRateAndReviewAppAsync();
            return result.Status == StoreRateAndReviewStatus.Succeeded;
        }
        catch
        {
            // Fallback mở Store protocol nếu chạy ngoài package hoặc API lỗi
            OpenStoreWebReview();
            return false;
        }
    }

    public static void OpenStoreWebReview()
    {
        try
        {
            // Mở trực tiếp trang đánh giá Store của app
            Process.Start(new ProcessStartInfo
            {
                FileName = "ms-windows-store://review/?PFN=MTDStudio.BFR-BatchFileRenamer_w3p168341p02a",
                UseShellExecute = true
            });
        }
        catch
        {
            try
            {
                // Fallback mở web Store
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://apps.microsoft.com/detail/MTDStudio.BFR-BatchFileRenamer",
                    UseShellExecute = true
                });
            }
            catch
            {
                // Bỏ qua lỗi nếu không thể mở trình duyệt
            }
        }
    }
}
