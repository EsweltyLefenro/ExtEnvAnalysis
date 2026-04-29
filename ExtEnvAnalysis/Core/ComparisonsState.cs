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

        // Единые цвета для точек на карте
        private static readonly SolidColorBrush MeBrush = CreateBrush(Color.FromRgb(33, 150, 243));   // синий
        private static readonly SolidColorBrush ABrush = CreateBrush(Color.FromRgb(255, 152, 0));    // оранжевый
        private static readonly SolidColorBrush BBrush = CreateBrush(Color.FromRgb(120, 144, 156));  // серый
        private static readonly SolidColorBrush CBrush = CreateBrush(Color.FromRgb(156, 39, 176));   // фиолетовый

        private static SolidColorBrush CreateBrush(Color c)
        {
            var b = new SolidColorBrush(c);
            b.Freeze(); // чтобы не плодить изменяемые объекты
            return b;
        }

        public void Rebuild(FactorsState factors, RatingsState ratings)
        {
            factors = factors ?? throw new System.ArgumentNullException(nameof(factors));
            ratings = ratings ?? throw new System.ArgumentNullException(nameof(ratings));
            var safeRatings = ratings!;

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
                    var xName = rX.Factor?.Name ?? "";
                    var yName = rY.Factor?.Name ?? "";

                    var map = new MapModel
                    {
                        Index = ++idx,
                        Count = total,
                        TitleX = xName,
                        TitleY = yName,

                        // подписи берём из ratings
                        Me = new CompanyPoint { Label = safeRatings.CompanyMyName, X = rX.MyValue, Y = rY.MyValue, Market = mMe, Brush = MeBrush },
                        A = new CompanyPoint { Label = safeRatings.CompanyAName, X = rX.AValue, Y = rY.AValue, Market = mA, Brush = ABrush },
                        B = new CompanyPoint { Label = safeRatings.CompanyBName, X = rX.BValue, Y = rY.BValue, Market = mB, Brush = BBrush },
                        C = new CompanyPoint { Label = safeRatings.CompanyCName, X = rX.CValue, Y = rY.CValue, Market = mC, Brush = CBrush },

                        Names = new[] { safeRatings.CompanyMyName, safeRatings.CompanyAName, safeRatings.CompanyBName, safeRatings.CompanyCName },

                        Direction = ""
                    };

                    Maps.Add(map);

                    map.Names = new[]
                    {
                        safeRatings.CompanyMyName,
                        safeRatings.CompanyAName,
                        safeRatings.CompanyBName,
                        safeRatings.CompanyCName
                    };
                }
        }
    }
}
