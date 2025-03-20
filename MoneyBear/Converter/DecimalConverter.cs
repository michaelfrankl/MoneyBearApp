using System;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using Avalonia.Data.Converters;

namespace MoneyBear.Converter;

public class DecimalConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal decimalValue)
        {
            return decimalValue.ToString("C2", culture);
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string stringValue)
        {
            stringValue = stringValue.Replace(culture.NumberFormat.CurrencySymbol, string.Empty).Trim();

            if (decimal.TryParse(stringValue, NumberStyles.Any | NumberStyles.AllowCurrencySymbol, culture, out var decimalValue))
            {
                return decimalValue;
            }
        }

        return 0m;
    }
}