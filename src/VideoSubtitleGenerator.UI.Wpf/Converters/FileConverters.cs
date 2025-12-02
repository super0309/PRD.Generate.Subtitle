using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using VideoSubtitleGenerator.Core;

namespace VideoSubtitleGenerator.UI.Wpf.Converters;

/// <summary>
/// Converts full file path to just the filename.
/// </summary>
public class FileNameConverter : IValueConverter
{
    /// <summary>
    /// Gets or sets whether to include the file extension.
    /// </summary>
    public bool IncludeExtension { get; set; } = true;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            if (value is string path && !string.IsNullOrEmpty(path))
            {
                try
                {
                    if (IncludeExtension)
                    {
                        return Path.GetFileName(path);
                    }
                    else
                    {
                        return Path.GetFileNameWithoutExtension(path);
                    }
                }
                catch
                {
                    return path; // Return original if parsing fails
                }
            }


        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("FileNameConverter does not support two-way binding.");
    }
}

/// <summary>
/// Converts file size in bytes to human-readable format (KB, MB, GB).
/// </summary>
public class FileSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return "0 B";

        long bytes = 0;

        if (value is long l)
            bytes = l;
        else if (value is int i)
            bytes = i;
        else if (value is double d)
            bytes = (long)d;
        else if (long.TryParse(value.ToString(), out var parsed))
            bytes = parsed;

        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("FileSizeConverter does not support two-way binding.");
    }
}

/// <summary>
/// Converts file path to directory path.
/// </summary>
public class PathToDirectoryConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string path && !string.IsNullOrEmpty(path))
        {
            try
            {
                return Path.GetDirectoryName(path) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("PathToDirectoryConverter does not support two-way binding.");
    }
}
