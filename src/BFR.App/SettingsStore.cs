namespace BFR.App;

using System;
using System.IO;
using System.Text.Json;

public sealed class AppSettings
{
    public string Language { get; set; } = "vi";

    /// <summary>
    /// Tự động kiểm tra bản cập nhật mới (chỉ áp dụng cho bản portable;
    /// bản cài từ Microsoft Store được Store cập nhật tự động).
    /// Mặc định TẮT theo nguyên tắc opt-in.
    /// </summary>
    public bool CheckForUpdates { get; set; } = false;

    /// <summary>URL nguồn kiểm tra phiên bản (API latest release của GitHub).</summary>
    public string UpdateFeedUrl { get; set; } = "https://api.github.com/repos/YOUR-ORG/BatchFileRenamer/releases/latest";

    /// <summary>Thư mục Watch Folder đang dùng (để mở lại).</summary>
    public string? WatchFolder { get; set; }

    /// <summary>Preset dùng cho Watch Folder.</summary>
    public string? WatchPreset { get; set; }

    /// <summary>Giao diện: "System", "Light" hoặc "Dark".</summary>

    public string Theme { get; set; } = "System";

    /// <summary>Giá hiển thị của Pro (dự phòng khi Store không trả giá). Không hard-code trong mã nguồn.</summary>
    public string? ProPriceText { get; set; }
}

/// <summary>
/// Lưu cài đặt người dùng (ngôn ngữ) vào %LocalAppData%\BatchFileRenamer\settings.json.
/// </summary>
public static class SettingsStore
{
    private static readonly string Directory_ = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BatchFileRenamer");

    private static readonly string SettingsPath = Path.Combine(Directory_, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new AppSettings();
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Directory_);
            var temp = SettingsPath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, SettingsPath, overwrite: true);
        }
        catch
        {
            // Cài đặt không phải dữ liệu quan trọng; bỏ qua lỗi ghi.
        }
    }
}
