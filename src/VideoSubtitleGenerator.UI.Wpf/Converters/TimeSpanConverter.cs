using System;
using System.Globalization;
using System.Windows.Data;

namespace VideoSubtitleGenerator.UI.Wpf.Converters;

/// <summary>
/// Converts TimeSpan to formatted string (HH:MM:SS or MM:SS).
/// </summary>
public class TimeSpanConverter : IValueConverter
{
    /// <summary>
    /// Gets or sets whether to always show hours, even if zero.
    /// </summary>
    public bool AlwaysShowHours { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TimeSpan timeSpan)
        {
            // Show hours if > 1 hour or AlwaysShowHours is true
            if (timeSpan.TotalHours >= 1 || AlwaysShowHours)
            {
                return timeSpan.ToString(@"hh\:mm\:ss");
            }
            else
            {
                return timeSpan.ToString(@"mm\:ss");
            }
        }

        if (value is int seconds)
        {
            return Convert(TimeSpan.FromSeconds(seconds), targetType, parameter, culture);
        }

        if (value is double totalSeconds)
        {
            return Convert(TimeSpan.FromSeconds(totalSeconds), targetType, parameter, culture);
        }

        return "00:00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && TimeSpan.TryParse(str, out var timeSpan))
        {
            return timeSpan;
        }

        return TimeSpan.Zero;
    }
}

/// <summary>
/// Converts TimeSpan to short format with appropriate unit (e.g., "2h 30m", "45m", "30s").
/// </summary>
public class TimeSpanToShortStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TimeSpan timeSpan)
        {
            if (timeSpan.TotalHours >= 1)
            {
                int hours = (int)timeSpan.TotalHours;
                int minutes = timeSpan.Minutes;
                return minutes > 0 ? $"{hours}h {minutes}m" : $"{hours}h";
            }
            else if (timeSpan.TotalMinutes >= 1)
            {
                int minutes = (int)timeSpan.TotalMinutes;
                int seconds = timeSpan.Seconds;
                return seconds > 0 ? $"{minutes}m {seconds}s" : $"{minutes}m";
            }
            else
            {
                return $"{(int)timeSpan.TotalSeconds}s";
            }
        }

        return "0s";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("TimeSpanToShortStringConverter does not support two-way binding.");
    }
}
