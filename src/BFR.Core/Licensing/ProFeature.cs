namespace BFR.Core.Licensing;

/// <summary>
/// Các tính năng bị khóa ở bản Free (PHẦN 3.6).
/// </summary>
public enum ProFeature
{
    /// <summary>Chuỗi quy tắc không giới hạn (Free: tối đa 3 quy tắc).</summary>
    UnlimitedRules,

    /// <summary>Ngày chụp EXIF ({taken}).</summary>
    ExifDate,

    /// <summary>Ngày tạo media (video/audio).</summary>
    MediaDate,

    /// <summary>Quét đệ quy không giới hạn cấp (Free: tối đa 2 cấp).</summary>
    UnlimitedRecursion,

    /// <summary>Lưu không giới hạn preset (Free: tối đa 3).</summary>
    UnlimitedPresets,

    /// <summary>Nhập/xuất tệp preset (.bfrpreset).</summary>
    PresetImportExport,

    /// <summary>Hoàn tác nhiều bước, lưu 30 ngày (Free: 1 bước gần nhất).</summary>
    MultiStepUndo,

    /// <summary>Xuất báo cáo CSV/JSON.</summary>
    ReportExport,

    /// <summary>Công cụ dòng lệnh (bfr.exe) cho tác vụ đổi tên hàng loạt.</summary>
    CommandLine,

    /// <summary>Đánh số kiểu La Mã.</summary>
    RomanNumbering
}
