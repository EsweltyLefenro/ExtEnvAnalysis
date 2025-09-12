using ExtEnvAnalysis.Core;
using ExtEnvAnalysis.Models;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Media;

namespace ExtEnvAnalysis
{
    public sealed class ComparisonsState
    {
        public ObservableCollection<MapModel> Maps { get; } = new();
        public bool IsValid => Maps.Count > 0; // есть хотя бы одна карта

        static readonly Brush MeBrush = Brushes.RoyalBlue;
        static readonly Brush ABrush = Brushes.Goldenrod;
        static readonly Brush BBrush = Brushes.MediumSeaGreen;
        static readonly Brush CBrush = Brushes.IndianRed;

        static double ParseOrZero(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            var ru = new CultureInfo("ru-RU");
            if (double.TryParse(s.Replace('.', ','), NumberStyles.Float, ru, out var v))
                return v;
            return 0;
        }

        // вызываем после любого изменения весов/оценок
        public void Rebuild(FactorsState factors, RatingsState ratings)
        {
            Maps.Clear();

            var rows = ratings.Rows
                .Where(r => r.IsActive)                // активные факторы (вес > 0 и имя есть)
                .ToList();
            if (rows.Count < 2) return;

            // доли рынка (0..100)
            var mMe = ParseOrZero(ratings.MarketMyText);
            var mA = ParseOrZero(ratings.MarketAText);
            var mB = ParseOrZero(ratings.MarketBText);
            var mC = ParseOrZero(ratings.MarketCText);

            int idx = 0, total = rows.Count * (rows.Count - 1) / 2;

            for (int i = 0; i < rows.Count - 1; i++)
                for (int j = i + 1; j < rows.Count; j++)
                {
                    var rX = rows[i];
                    var rY = rows[j];

                    var map = new MapModel
                    {
                        Index = ++idx,
                        Count = total,
                        TitleX = rX.Factor.Name,
                        TitleY = rY.Factor.Name,

                        Me = new CompanyPoint { Label = "Мы", X = rX.MyValue, Y = rY.MyValue, Market = mMe, Brush = MeBrush },
                        A = new CompanyPoint { Label = "A", X = rX.AValue, Y = rY.AValue, Market = mA, Brush = ABrush },
                        B = new CompanyPoint { Label = "B", X = rX.BValue, Y = rY.BValue, Market = mB, Brush = BBrush },
                        C = new CompanyPoint { Label = "C", X = rX.CValue, Y = rY.CValue, Market = mC, Brush = CBrush },
                        Explanation = $"X: {rX.Factor.Name}\nY: {rY.Factor.Name}\nПодсказка: отметьте лидеров по осям и зоны роста.",
                        Direction = ""
                    };

                    Maps.Add(map);
                }
        }
    }
}
