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
        private bool _isRecalcRunning;

        public ObservableCollection<RatingRow> Rows { get; } = new();

        [ObservableProperty] private bool isValid;
        [ObservableProperty] private double totalMy;
        [ObservableProperty] private double totalA;
        [ObservableProperty] private double totalB;
        [ObservableProperty] private double totalC;

        // Доля рынка (в процентах 0..100). Участвует в IsValid, чтобы карты
        // и отчет строились только по логически корректным данным.
        [ObservableProperty] private string? marketMyText;
        [ObservableProperty] private string? marketAText;
        [ObservableProperty] private string? marketBText;
        [ObservableProperty] private string? marketCText;

        partial void OnMarketMyTextChanged(string? value) => Recalculate();
        partial void OnMarketATextChanged(string? value) => Recalculate();
        partial void OnMarketBTextChanged(string? value) => Recalculate();
        partial void OnMarketCTextChanged(string? value) => Recalculate();

        public void AttachToFactors(FactorsState factors)
        {
            if (ReferenceEquals(_factors, factors))
            {
                SyncInternal();
                return;
            }

            if (_factors != null)
            {
                _factors.Rows.CollectionChanged -= FactorsRows_CollectionChanged;
                foreach (var r in _factors.Rows)
                    r.PropertyChanged -= Factor_PropertyChanged;
            }

            _factors = factors;
            _factors.Rows.CollectionChanged += FactorsRows_CollectionChanged;
            foreach (var r in _factors.Rows)
                r.PropertyChanged += Factor_PropertyChanged;

            SyncInternal();
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
                SyncInternal();
            }
        }

        private void FactorsRows_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (FactorRow r in e.OldItems) r.PropertyChanged -= Factor_PropertyChanged;

            if (e.NewItems != null)
                foreach (FactorRow r in e.NewItems) r.PropertyChanged += Factor_PropertyChanged;

            SyncInternal();
        }

        private void Row_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(RatingRow.MyText) or nameof(RatingRow.AText)
                or nameof(RatingRow.BText) or nameof(RatingRow.CText))
            {
                Recalculate();
            }
        }

        public void SyncWithFactors(FactorsState factors)
        {
            if (factors is null) return;
            AttachToFactors(factors);
        }

        private void SyncInternal()
        {
            if (_factors == null) return;

            // кэш уже введённых оценок по ссылке на фактор
            var cache = Rows.ToDictionary(r => r.Factor, r => r);

            // берём ТОЛЬКО активные факторы: имя есть и вес > 0
            var active = _factors.Rows
                .Where(f => !string.IsNullOrWhiteSpace(f.Name) && f.WeightValue > 0)
                .ToList();

            var newRows = new ObservableCollection<RatingRow>();

            foreach (var f in active)
            {
                RatingRow row;

                if (cache.TryGetValue(f, out var old))
                {
                    // есть старая строка — сохраняем оценки, обновим ссылку/вес
                    row = old;
                }
                else
                {
                    // новая активная строка — пустые оценки
                    row = new RatingRow(f);
                }

                // на всякий случай обновим ссылку на фактор (если конструктор не делал)
                row.Factor = f;

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
            if (_isRecalcRunning) return;      // защита от повторного входа
            _isRecalcRunning = true;
            try
            {
                double sumW = 0.0;
                double numMy = 0.0, numA = 0.0, numB = 0.0, numC = 0.0;

                foreach (var r in Rows)
                {
                    var w = r.Factor?.WeightValue ?? 0.0;
                    if (w <= 0) continue;

                    sumW += w;

                    int sMy = ParseScore(r.MyText);
                    int sA = ParseScore(r.AText);
                    int sB = ParseScore(r.BText);
                    int sC = ParseScore(r.CText);

                    numMy += w * sMy;
                    numA += w * sA;
                    numB += w * sB;
                    numC += w * sC;
                }

                if (sumW > 0)
                {
                    TotalMy = numMy / sumW;   // [ObservableProperty] сам поднимет PropertyChanged
                    TotalA = numA / sumW;
                    TotalB = numB / sumW;
                    TotalC = numC / sumW;
                }
                else
                {
                    TotalMy = TotalA = TotalB = TotalC = 0.0;
                }

                // валидность для CanTab5
                // фиксируем набор, чтобы коллекция не "живая" в условиях и не провоцировала повторный пересчёт
                var active = Rows.Where(r => (r.Factor?.WeightValue ?? 0) > 0).ToList();
                bool hasActive = active.Count > 0;
                bool allScoresValid = active.All(r => IsScore(r.MyText) && IsScore(r.AText)
                                                    && IsScore(r.BText) && IsScore(r.CText));
                bool sharesValid = AreMarketSharesValid();
                IsValid = hasActive && allScoresValid && sharesValid;  // [ObservableProperty] сам поднимет PropertyChanged
            }
            finally
            {
                _isRecalcRunning = false;
            }
        }

        // утилиты
        private static int ParseScore(string? s)
        {
            return TryScore(s, out var v) ? v : 0;
        }

        private static bool IsScore(string? s) =>
            int.TryParse(s, out var v) && v >= 1 && v <= 10;


        // --- алиасы для совместимости с AppState ---
        public void RecalculateTotals() => Recalculate();
        public void Touch() => Recalculate();

        internal static bool TryScore(string? t, out int v)
        {
            v = 0;
            if (string.IsNullOrWhiteSpace(t)) return false;
            if (!int.TryParse(t.Trim(), out v)) return false;
            return v >= 1 && v <= 10;
        }

        internal static bool TryPercent(string? t)
        {
            if (string.IsNullOrWhiteSpace(t)) return false;
            if (!int.TryParse(t.Trim(), out var v)) return false;
            return v >= 0 && v <= 100;
        }

        public bool AreMarketSharesValid()
        {
            if (!TryPercent(MarketMyText) ||
                !TryPercent(MarketAText) ||
                !TryPercent(MarketBText) ||
                !TryPercent(MarketCText))
            {
                return false;
            }

            var shares = GetShares01();
            return shares.Sum() <= 1.0 + 1e-9;
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
