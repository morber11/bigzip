using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace BigZipUI.Converters
{
    // used in MainWindow.axaml
    public class BoolToOpacityConverter : IValueConverter
    {
        // awkward way to keep the size consistent by hiding the opacity of the progress bar
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return (bool)(value ?? false) ? 1.0 : 0.0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
