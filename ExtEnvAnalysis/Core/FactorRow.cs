using CommunityToolkit.Mvvm.ComponentModel;
using System.Globalization;

namespace ExtEnvAnalysis.Core
{
    /// Строка таблицы факторов.
    /// WeightText — как вводит пользователь (может быть пустой, с запятой),
    /// WeightValue — нормализованное значение 0..1 (double), округлённое до сотых.
    public partial class FactorRow : ObservableObject
    {
        [ObservableProperty] private int index;
        [ObservableProperty] private string? name;
        [ObservableProperty] private string? weightText; // "", "0,5", "0.5" и т.п.

        public string SuggestedName => $"Фактор {(char)('A' + (Index % 26))}";

        public double WeightValue
        {
            get
            {
                return TryParseWeight(WeightText, out var v) ? v : 0.0;
            }
        }

        public static bool TryParseWeight(string? text, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;

            var s = text.Trim();
            // допускаем запятую и точку, пробелы не допускаем
            s = s.Replace(',', '.');

            if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                return false;

            // диапазон 0..1
            if (v < 0) v = 0;
            if (v > 1) v = 1;

            value = System.Math.Round(v, 2, MidpointRounding.AwayFromZero);
            return true;
        }

        /// <summary>
        /// Приводит текст к формату "0,00" (ru-RU). Пустую строку не трогаем.
        /// </summary>
        public void FormatWeightText()
        {
            if (TryParseWeight(WeightText, out var v))
                WeightText = v.ToString("0.00", new CultureInfo("ru-RU"));
        }

        // --- Back-compat со старым кодом ---
        public string? Weight
        {
            get => WeightText;
            set => WeightText = value;
        }

        public void SetWeightFromText(string? text)
        {
            WeightText = text;
            FormatWeightText();
        }
        // --- конец совместимости ---
    }
}
