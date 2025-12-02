using System;
using System.Globalization;
using System.Windows.Data;

namespace VideoSubtitleGenerator.UI.Wpf.Converters;

/// <summary>
/// Converts progress percentage (0-100) to width for progress bars.
/// </summary>
public class ProgressToWidthConverter : IValueConverter
{
    /// <summary>
    /// Gets or sets the maximum width (100% progress).
    /// </summary>
    public double MaxWidth { get; set; } = 300;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double progress = 0;

        if (value is double d)
            progress = d;
        else if (value is int i)
            progress = i;
        else if (value is float f)
            progress = f;
        else if (double.TryParse(value?.ToString(), out var parsed))
            progress = parsed;

        // If parameter is specified, use it as MaxWidth
        if (parameter != null && double.TryParse(parameter.ToString(), out var maxWidth))
        {
            MaxWidth = maxWidth;
        }

        // Clamp between 0 and 100
        progress = Math.Max(0, Math.Min(100, progress));

        return (progress / 100.0) * MaxWidth;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("ProgressToWidthConverter does not support two-way binding.");
    }
}

/// <summary>
/// Converts progress (0-100) to formatted percentage string.
/// </summary>
public class ProgressToPercentageConverter : IValueConverter
{
    /// <summary>
    /// Gets or sets the number of decimal places to show.
    /// </summary>
    public int DecimalPlaces { get; set; } = 0;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double progress = 0;

        if (value is double d)
            progress = d;
        else if (value is int i)
            progress = i;
        else if (value is float f)
            progress = f;
        else if (double.TryParse(value?.ToString(), out var parsed))
            progress = parsed;

        progress = Math.Max(0, Math.Min(100, progress));

        string format = DecimalPlaces > 0 ? $"F{DecimalPlaces}" : "F0";
        return $"{progress.ToString(format)}%";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            str = str.TrimEnd('%', ' ');
            if (double.TryParse(str, out var result))
            {
                return result;
            }
        }

        return 0.0;
    }
}

/// <summary>
/// Inverts a boolean value.
/// </summary>
public class InvertBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;

        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;

        return false;
    }
}

/// <summary>
/// Returns true if value is null or empty string.
/// </summary>
public class IsNullOrEmptyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return true;

        if (value is string str)
            return string.IsNullOrWhiteSpace(str);

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("IsNullOrEmptyConverter does not support two-way binding.");
    }
}
