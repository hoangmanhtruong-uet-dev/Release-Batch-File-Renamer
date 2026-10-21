namespace BFR.Presentation;

using System.Globalization;

/// <summary>
/// Nguồn chuỗi hiển thị cho ViewModel. Tầng UI (WPF) cài bản đọc từ resource 4 ngôn ngữ;
/// bản mặc định trả về chính khoá để unit test không phụ thuộc WPF.
/// </summary>
public interface IStringProvider
{
    /// <summary>Chuỗi theo khoá.</summary>
    string T(string key);

    /// <summary>Chuỗi có tham số định dạng.</summary>
    string F(string key, params object[] args);

    /// <summary>Chuỗi song ngữ theo ngôn ngữ hiện tại.</summary>
    string L(string vi, string en);
}

/// <summary>Bản tối giản: trả về chính khoá (hữu ích khi test hoặc khi thiếu resource).</summary>
public sealed class KeyOnlyStringProvider : IStringProvider
{
    public string T(string key) => key;

    /// <summary>
    /// Trả về khoá kèm tham số dưới dạng "Key(arg1, arg2)" để test vẫn kiểm tra được
    /// nội dung thực sự được ghép vào chuỗi.
    /// </summary>
    public string F(string key, params object[] args) =>
        args.Length == 0
            ? key
            : key + "(" + string.Join(", ", args.Select(a => a?.ToString() ?? string.Empty)) + ")";

    public string L(string vi, string en) => vi;
}
