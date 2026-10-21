namespace BFR.App;

using BFR.Core;
using System;
using System.Globalization;
using System.Windows.Data;

/// <summary>
/// Hiển thị trạng thái preview bằng ngôn ngữ đang chọn thay vì tên enum tiếng Anh.
/// </summary>
public sealed class StatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PreviewStatus status)
        {
            return Loc.T($"Status{status}");
        }

        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
