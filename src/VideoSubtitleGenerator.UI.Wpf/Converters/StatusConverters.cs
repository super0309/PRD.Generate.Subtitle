using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using VideoSubtitleGenerator.Core.Enums;

namespace VideoSubtitleGenerator.UI.Wpf.Converters;

/// <summary>
/// Converts JobStatus enum to Color for status indicators.
/// </summary>
public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is JobStatus status)
        {
            return status switch
            {
                JobStatus.Pending => Color.FromRgb(158, 158, 158),      // Gray
                JobStatus.Converting => Color.FromRgb(33, 150, 243),    // Blue
                JobStatus.Transcribing => Color.FromRgb(156, 39, 176),  // Purple
                JobStatus.Completed => Color.FromRgb(76, 175, 80),      // Green
                JobStatus.Failed => Color.FromRgb(244, 67, 54),         // Red
                JobStatus.Canceled => Color.FromRgb(255, 152, 0),       // Orange
                _ => Colors.Gray
            };
        }

        return Colors.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("StatusToColorConverter does not support two-way binding.");
    }
}

/// <summary>
/// Converts JobStatus enum to SolidColorBrush for direct binding to WPF controls.
/// </summary>
public class StatusToBrushConverter : IValueConverter
{
    private static readonly StatusToColorConverter _colorConverter = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var color = (Color)_colorConverter.Convert(value, typeof(Color), parameter, culture);
        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("StatusToBrushConverter does not support two-way binding.");
    }
}

/// <summary>
/// Converts JobStatus enum to localized status text.
/// </summary>
public class StatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is JobStatus status)
        {
            return status switch
            {
                JobStatus.Pending => "Đang chờ",
                JobStatus.Converting => "Đang chuyển đổi",
                JobStatus.Transcribing => "Đang phiên âm",
                JobStatus.Completed => "Hoàn thành",
                JobStatus.Failed => "Thất bại",
                JobStatus.Canceled => "Đã hủy",
                _ => "Không xác định"
            };
        }

        return "Không xác định";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("StatusToTextConverter does not support two-way binding.");
    }
}
