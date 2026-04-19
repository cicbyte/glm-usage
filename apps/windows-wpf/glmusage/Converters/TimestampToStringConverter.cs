using System.Globalization;
using System.Windows.Data;

namespace glmusage.Converters
{
    public class TimestampToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long timestamp && timestamp > 0)
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(timestamp)
                    .ToLocalTime().ToString("M/d HH:mm");
            }
            return "--";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
