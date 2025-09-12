using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ExtEnvAnalysis.Core;

namespace ExtEnvAnalysis.Converters
{
    public class WatermarkVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3) return Visibility.Collapsed;

            // 0: Difficulty (enum или строка)
            Difficulty level;
            if (values[0] is Difficulty d) level = d;
            else if (values[0] is string s && Enum.TryParse<Difficulty>(s, true, out var d2)) level = d2;
            else return Visibility.Collapsed;

            // 1: Text, 2: IsKeyboardFocused
            string text = values[1]?.ToString() ?? "";
            bool isFocused = values[2] is bool b && b;

            bool visible = (level == Difficulty.Bachelor) &&
                           string.IsNullOrWhiteSpace(text) &&
                           !isFocused;

            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
