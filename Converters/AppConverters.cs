using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using NetPulse.Models;

namespace NetPulse.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; } = false;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool b = value is true;
        if (Invert) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class NullToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; } = false;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isNull = value == null;
        if (value is string s) isNull = string.IsNullOrWhiteSpace(s);
        bool visible = Invert ? isNull : !isNull;
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class QualityGradeToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is NetworkQualityGrade grade)
        {
            byte a = (byte)((grade.ColorHex >> 24) & 0xFF);
            byte r = (byte)((grade.ColorHex >> 16) & 0xFF);
            byte g = (byte)((grade.ColorHex >> 8) & 0xFF);
            byte b = (byte)(grade.ColorHex & 0xFF);
            return new SolidColorBrush(Color.FromArgb(a, r, g, b));
        }

        return new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
