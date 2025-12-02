using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using VideoSubtitleGenerator.Core;

namespace VideoSubtitleGenerator.UI.Wpf.Converters;

/// <summary>
/// Converts boolean values to Visibility enum for controlling element visibility.
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Gets or sets whether true should map to Collapsed instead of Visible (inverted logic).
    /// </summary>
    public bool IsInverted { get; set; }

    /// <summary>
    /// Gets or sets whether false should map to Hidden instead of Collapsed.
    /// </summary>
    public bool UseHidden { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            bool boolValue = false;

            if (value is bool b)
                boolValue = b;
            else if (value != null)
                bool.TryParse(value.ToString(), out boolValue);

            // Apply inversion if specified
            if (IsInverted)
                boolValue = !boolValue;

            if (boolValue)
                return Visibility.Visible;
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            //return false;
        }
        return UseHidden ? Visibility.Hidden : Visibility.Collapsed;

    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            bool result = visibility == Visibility.Visible;
            return IsInverted ? !result : result;
        }

        return false;
    }
}

/// <summary>
/// Inverted version: false = Visible, true = Collapsed
/// </summary>
public class InvertedBoolToVisibilityConverter : BoolToVisibilityConverter
{
    public InvertedBoolToVisibilityConverter()
    {
        IsInverted = true;
    }
}
