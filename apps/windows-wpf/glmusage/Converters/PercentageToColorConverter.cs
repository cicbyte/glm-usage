using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace glmusage.Converters
{
    public class PercentageToColorConverter : IValueConverter
    {
        public int WarningThreshold { get; set; } = 70;
        public int DangerThreshold { get; set; } = 80;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int percentage)
            {
                if (percentage >= DangerThreshold)
                    return new SolidColorBrush(Color.FromRgb(0xE7, 0x4C, 0x3C));
                if (percentage >= WarningThreshold)
                    return new SolidColorBrush(Color.FromRgb(0xF1, 0xC4, 0x0F));
                return new SolidColorBrush(Color.FromRgb(0x2E, 0xCC, 0x71));
            }
            return new SolidColorBrush(Color.FromRgb(0x2E, 0xCC, 0x71));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
