using System;
using System.Globalization;
using System.Windows.Data;

namespace ExtEnvAnalysis.Converters
{
    // true, если value[0] == value[1] (Tag кнопки == выбранный сегмент)
    public class SegmentSelectedConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return false;
            var tag = values[0]?.ToString();
            var selected = values[1]?.ToString();
            return string.Equals(tag, selected, StringComparison.Ordinal);
        }

        // Важно: ничего не пишем обратно, чтобы исключений не было.
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return targetTypes.Select(_ => Binding.DoNothing).ToArray();
        }
    }
}
