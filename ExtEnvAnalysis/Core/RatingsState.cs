using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace ExtEnvAnalysis.Core
{
    public partial class RatingsState : ObservableObject, ISection
    {
        private FactorsState? _factors;

        public ObservableCollection<RatingRow> Rows { get; } = new();

        [ObservableProperty] private bool isValid;
        [ObservableProperty] private double totalMy;
        [ObservableProperty] private double totalA;
        [ObservableProperty] private double totalB;
        [ObservableProperty] private double totalC;

        // Доля рынка (в процентах 0..100, НЕ влияет на IsValid)
        [ObservableProperty] private string? marketMyText;
        [ObservableProperty] private string? marketAText;
        [ObservableProperty] private string? marketBText;
        [ObservableProperty] private string? marketCText;

        public void AttachToFactors(FactorsState factors)
        {
            _factors = factors;
            SyncWithFactors();

            factors.Rows.CollectionChanged += (_, __) => SyncWithFactors();
            foreach (var r in factors.Rows) r.PropertyChanged += Factor_PropertyChanged;
        }

        public void Reset()
        {
            Rows.Clear();
            IsValid = false;
            TotalMy = TotalA = TotalB = TotalC = 0;
            MarketMyText = MarketAText = MarketBText = MarketCText = "";
        }

        private void Factor_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FactorRow.Name) ||
                e.PropertyName == nameof(FactorRow.WeightText))
            {
                Recalculate();
            }
        }

        private void Row_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(RatingRow.MyText) or nameof(RatingRow.AText)
                or nameof(RatingRow.BText) or nameof(RatingRow.CText))
            {
                Recalculate();
            }
        }

        private void SyncWithFactors()
        {
            if (_factors == null) return;

            // Пересобираем список строк под факторы один-в-один (в том же порядке)
            var newRows = new ObservableCollection<RatingRow>();
            foreach (var f in _factors.Rows)
            {
                var existing = Rows.FirstOrDefault(r => ReferenceEquals(r.Factor, f));
                var row = existing ?? new RatingRow(f);
                // подписка на изменения оценок
                row.PropertyChanged -= Row_PropertyChanged;
                row.PropertyChanged += Row_PropertyChanged;
                newRows.Add(row);
            }

            Rows.CollectionChanged -= Rows_CollectionChanged;
            Rows.Clear();
            foreach (var r in newRows) Rows.Add(r);
            Rows.CollectionChanged += Rows_CollectionChanged;

            Recalculate();
        }

        private void Rows_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (RatingRow r in e.OldItems) r.PropertyChanged -= Row_PropertyChanged;
            if (e.NewItems != null)
                foreach (RatingRow r in e.NewItems) r.PropertyChanged += Row_PropertyChanged;
        }

        public void Recalculate()
        {
            double sMy = 0, sA = 0, sB = 0, sC = 0;
            bool ok = true;

            foreach (var row in Rows)
            {
                var w = row.Factor.WeightValue;
                var active = row.IsActive;

                if (active)
                {
                    if (!(TryScore(row.MyText, out int my) &&
                          TryScore(row.AText, out int a) &&
                          TryScore(row.BText, out int b) &&
                          TryScore(row.CText, out int c)))
                    {
                        ok = false; // активная строка — все 4 оценки обязательны (1..10)
                    }
                    else
                    {
                        sMy += my * w;
                        sA += a * w;
                        sB += b * w;
                        sC += c * w;
                    }
                }
                // неактивная строка (вес 0 или пустое имя) не участвует и не требует оценок
            }

            TotalMy = Math.Round(sMy, 2, MidpointRounding.AwayFromZero);
            TotalA = Math.Round(sA, 2, MidpointRounding.AwayFromZero);
            TotalB = Math.Round(sB, 2, MidpointRounding.AwayFromZero);
            TotalC = Math.Round(sC, 2, MidpointRounding.AwayFromZero);

            // проверяем «Долю рынка» — обязательные целые 0..100
            bool marketOk =
                TryPercent(MarketMyText) &&
                TryPercent(MarketAText) &&
                TryPercent(MarketBText) &&
                TryPercent(MarketCText);

            // Шаг 5 валиден, если заполнены все активные оценки И корректна «Доля рынка»
            IsValid = ok && marketOk;
        }

        // --- алиасы для совместимости с AppState ---
        public void RecalculateTotals() => Recalculate();
        public void Touch() => Recalculate();

        internal static bool TryScore(string? t, out int v)
        {
            v = 0;
            if (string.IsNullOrWhiteSpace(t)) return false;
            if (!int.TryParse(t.Trim(), out v)) return false;
            if (v < 1) v = 1;
            if (v > 10) v = 10;
            return true;
        }

        internal static bool TryPercent(string? t)
        {
            if (string.IsNullOrWhiteSpace(t)) return false;
            if (!int.TryParse(t.Trim(), out var v)) return false;
            return v >= 0 && v <= 100;
        }

        // ===== ДОЛИ РЫНКА ДЛЯ ОТЧЁТА =====

        private static double ParsePercent01(string? t)
        {
            if (!TryPercent(t)) return 0.0;
            _ = int.TryParse(t!.Trim(), out var v);
            if (v < 0) v = 0; if (v > 100) v = 100;
            return v / 100.0;
        }

        public double[] GetShares01()
        {
            int v;
            double clamp01(string? s)
            {
                if (!int.TryParse((s ?? "").Trim(), out v)) v = 0;
                if (v < 0) v = 0; if (v > 100) v = 100;
                return v / 100.0;
            }
            return new[]
            {
                clamp01(MarketMyText),
                clamp01(MarketAText),
                clamp01(MarketBText),
                clamp01(MarketCText)
            };
        }

        // === Названия компаний (редактируемые пользователем) ===
        private string _companyMyName = "Мы";
        public string CompanyMyName
        {
            get => _companyMyName;
            set { if (_companyMyName != value) { _companyMyName = value; OnPropertyChanged(nameof(CompanyMyName)); } }
        }

        private string _companyAName = "Компания 1";
        public string CompanyAName
        {
            get => _companyAName;
            set { if (_companyAName != value) { _companyAName = value; OnPropertyChanged(nameof(CompanyAName)); } }
        }

        private string _companyBName = "Компания 2";
        public string CompanyBName
        {
            get => _companyBName;
            set { if (_companyBName != value) { _companyBName = value; OnPropertyChanged(nameof(CompanyBName)); } }
        }

        private string _companyCName = "Компания 3";
        public string CompanyCName
        {
            get => _companyCName;
            set { if (_companyCName != value) { _companyCName = value; OnPropertyChanged(nameof(CompanyCName)); } }
        }


        public void ApplyPresetDeveloper()
        {
            // 1) Доля рынка — заполняем ТОЛЬКО если пусто
            if (string.IsNullOrWhiteSpace(MarketMyText)) MarketMyText = "20";
            if (string.IsNullOrWhiteSpace(MarketAText)) MarketAText = "30";
            if (string.IsNullOrWhiteSpace(MarketBText)) MarketBText = "25";
            if (string.IsNullOrWhiteSpace(MarketCText)) MarketCText = "25";

            // 2) Оценки по факторам — для всех незаполненных ячеек ставим случайные целые 1..10
            var rnd = new Random(Environment.TickCount);
            string Next() => rnd.Next(1, 11).ToString();

            foreach (var row in Rows)
            {
                // Если хочешь только по «активным» факторам — оставь условие:
                // if (!row.IsActive) continue;

                if (string.IsNullOrWhiteSpace(row.MyText)) row.MyText = Next();
                if (string.IsNullOrWhiteSpace(row.AText)) row.AText = Next();
                if (string.IsNullOrWhiteSpace(row.BText)) row.BText = Next();
                if (string.IsNullOrWhiteSpace(row.CText)) row.CText = Next();
            }

            // 3) Пересчитать итоги валидности/сумм
            Recalculate();
        }

    }
}
