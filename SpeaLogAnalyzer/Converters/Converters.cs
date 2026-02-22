using System.Globalization;
using SpeaLogAnalyzer.Models;

namespace SpeaLogAnalyzer.Converters;

public class TestResultToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TestResult result)
        {
            return result switch
            {
                TestResult.Pass => Colors.Green,
                TestResult.Fail or TestResult.FailHigh or TestResult.FailLow => Colors.Red,
                TestResult.None => Colors.Gray,
                _ => Colors.Gray
            };
        }

        if (value is string str)
        {
            return str.ToUpperInvariant() switch
            {
                "PASS" => Colors.Green,
                "FAIL" or "FAIL(+)" or "FAIL(-)" or "FAIL(+/-)" => Colors.Red,
                _ => Colors.Gray
            };
        }

        return Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class TestResultToBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TestResult result)
        {
            return result switch
            {
                TestResult.Pass => Color.FromArgb("#E8F5E9"),
                TestResult.Fail or TestResult.FailHigh or TestResult.FailLow => Color.FromArgb("#FFEBEE"),
                _ => Colors.Transparent
            };
        }

        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class InverseBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b ? !b : value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b ? !b : value;
    }
}

public class PercentToDoubleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int i)
            return i / 100.0;
        if (value is double d)
            return d / 100.0;
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
