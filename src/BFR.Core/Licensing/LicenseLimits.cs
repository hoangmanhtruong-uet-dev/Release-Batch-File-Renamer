namespace BFR.Core.Licensing;

/// <summary>
/// Giới hạn theo từng cấp giấy phép. Giá trị <see cref="int.MaxValue"/> nghĩa là không giới hạn.
/// </summary>
public sealed record LicenseLimits
{
    /// <summary>Số quy tắc tối đa trong một chuỗi quy tắc.</summary>
    public int MaxRules { get; init; }

    /// <summary>Số preset người dùng tối đa được lưu trong thư viện.</summary>
    public int MaxSavedPresets { get; init; }

    /// <summary>Số lần áp dụng tối đa còn trong lịch sử để hoàn tác.</summary>
    public int MaxUndoEntries { get; init; }

    /// <summary>Độ sâu quét thư mục con tối đa (0 = chỉ thư mục gốc).</summary>
    public int MaxRecursionDepth { get; init; }

    /// <summary>Cho phép dùng ngày chụp EXIF.</summary>
    public bool AllowExifDate { get; init; }

    /// <summary>Cho phép dùng ngày tạo media (video/audio).</summary>
    public bool AllowMediaDate { get; init; }

    /// <summary>Cho phép nhập/xuất tệp preset.</summary>
    public bool AllowPresetImportExport { get; init; }

    /// <summary>Cho phép xuất báo cáo CSV/JSON.</summary>
    public bool AllowReportExport { get; init; }

    /// <summary>Cho phép dùng CLI cho tác vụ đổi tên.</summary>
    public bool AllowCommandLine { get; init; }

    /// <summary>Cho phép đánh số kiểu La Mã.</summary>
    public bool AllowRomanNumbering { get; init; }

    /// <summary>Giới hạn của bản miễn phí (PHẦN 3.6).</summary>
    public static LicenseLimits Free { get; } = new()
    {
        MaxRules = 3,
        MaxSavedPresets = 3,
        MaxUndoEntries = 1,
        MaxRecursionDepth = 2,
        AllowExifDate = false,
        AllowMediaDate = false,
        AllowPresetImportExport = false,
        AllowReportExport = false,
        AllowCommandLine = false,
        AllowRomanNumbering = false
    };

    /// <summary>Giới hạn của bản Pro: không giới hạn.</summary>
    public static LicenseLimits Pro { get; } = new()
    {
        MaxRules = int.MaxValue,
        MaxSavedPresets = int.MaxValue,
        MaxUndoEntries = int.MaxValue,
        MaxRecursionDepth = int.MaxValue,
        AllowExifDate = true,
        AllowMediaDate = true,
        AllowPresetImportExport = true,
        AllowReportExport = true,
        AllowCommandLine = true,
        AllowRomanNumbering = true
    };

    public static LicenseLimits For(LicenseTier tier) =>
        tier == LicenseTier.Pro ? Pro : Free;

    /// <summary>True nếu giá trị <paramref name="value"/> vượt quá giới hạn đã cho.</summary>
    public bool Exceeds(int limit, int value) => value > limit;
}
