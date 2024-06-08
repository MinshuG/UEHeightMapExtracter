using System;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Data.Converters;

namespace HeightMapExtractorGUI.ViewModels.Converters;

public class DataSizeConverter: IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is long size)
        {
            if (size < 1024) return $"{size} B";
            if (size < 1024 * 1024) return $"{size / 1024.0:F2} KB";
            if (size < 1024 * 1024 * 1024) return $"{size / 1024.0 / 1024.0:F2} MB";
            return $"{size / 1024.0 / 1024.0 / 1024.0:F2} GB";    
        }
        Debug.Assert(false, "DataSizeConverter: Invalid size");
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}