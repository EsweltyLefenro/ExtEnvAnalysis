using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Media;
using ExtEnvAnalysis.Models;

namespace ExtEnvAnalysis.Core
{
    public sealed class ComparisonsState : ISection
    {
        public ObservableCollection<MapModel> Maps { get; } = new();

        public bool IsValid => Maps.Count > 0;

        public void Reset() => Maps.Clear();

        static double ParsePercent(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            var ru = new CultureInfo("ru-RU");
            return double.TryParse(s.Replace('.', ','), NumberStyles.Float, ru, out var v) ? v : 0;
        }

        public void Rebuild(FactorsState factors, RatingsState ratings)
        {
            Maps.Clear();

            // Берём только активные строки (вес > 0 и есть имя фактора)
            var rows = ratings.Rows.Where(r => r.IsActive).ToList();
            if (rows.Count < 2) return;

            // Доли рынка (0..100) — влияют на размер пузыря
            double mMe = ParsePercent(ratings.MarketMyText);
            double mA = ParsePercent(ratings.MarketAText);
            double mB = ParsePercent(ratings.MarketBText);
            double mC = ParsePercent(ratings.MarketCText);

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

                        Me = new CompanyPoint { Label = "Мы", X = rX.MyValue, Y = rY.MyValue, Market = mMe, Brush = Brushes.RoyalBlue },
                        A = new CompanyPoint { Label = "A", X = rX.AValue, Y = rY.AValue, Market = mA, Brush = Brushes.Goldenrod },
                        B = new CompanyPoint { Label = "B", X = rX.BValue, Y = rY.BValue, Market = mB, Brush = Brushes.MediumSeaGreen },
                        C = new CompanyPoint { Label = "C", X = rX.CValue, Y = rY.CValue, Market = mC, Brush = Brushes.IndianRed },

                        Explanation = $"X: {rX.Factor.Name}\nY: {rY.Factor.Name}",
                        Direction = ""
                    };

                    Maps.Add(map);
                }
        }
    }
}
