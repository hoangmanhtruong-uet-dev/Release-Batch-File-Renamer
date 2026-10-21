namespace BFR.Core.Licensing;

/// <summary>
/// Cấp giấy phép của ứng dụng (PHẦN 3.6 — phân tách Free và Pro).
/// </summary>
public enum LicenseTier
{
    /// <summary>Bản miễn phí: giới hạn số quy tắc, độ sâu quét, preset, undo 1 bước.</summary>
    Free = 0,

    /// <summary>Bản Pro: mở khoá toàn bộ tính năng nâng cao (mua đứt trọn đời).</summary>
    Pro = 1
}
