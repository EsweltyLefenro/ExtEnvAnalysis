using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ExtEnvAnalysis.Converters
{
    public class SumOkToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double v = 0;
            if (value is double d) v = d;
            var ok = Math.Abs(v - 1.0) < 0.0001;
            return ok ? Brushes.SeaGreen : Brushes.IndianRed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
