using System.Globalization;
using System.Windows.Data;

namespace glmusage.Converters
{
    public class PercentageToWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is int percentage && values[1] is double totalWidth)
            {
                return Math.Max(0, totalWidth * Math.Min(percentage, 100) / 100.0);
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
