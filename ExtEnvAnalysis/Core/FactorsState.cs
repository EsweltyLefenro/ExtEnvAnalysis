using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace ExtEnvAnalysis.Core
{
    public partial class FactorsState : ObservableObject, ISection
    {
        public ObservableCollection<FactorRow> Rows { get; } = new();

        [ObservableProperty] private bool isValid;
        [ObservableProperty] private double sum; // сумма 0..1
        private Difficulty _lastLevel = Difficulty.Bachelor;

        public FactorsState()
        {
            Rows.CollectionChanged += Rows_CollectionChanged;
        }

        public void InitForDifficulty(Difficulty level)
        {
            Rows.Clear();
            IsValid = false;
            Sum = 0;
        }

        public void ApplyPresetForBachelor(string? segmentName)
        {
            Rows.Clear();

            string[] names;
            if (!string.IsNullOrWhiteSpace(segmentName) &&
                FactorSeeds.BySegment.TryGetValue(segmentName, out var arr) && arr != null && arr.Length > 0)
                names = arr.Take(10).ToArray();
            else
                names = Enumerable.Range(0, 10).Select(i => $"Фактор {(char)('A' + i)}").ToArray();

            for (int i = 0; i < 10; i++)
            {
                Rows.Add(new FactorRow
                {
                    Index = i,
                    Name = names[i],
                    WeightText = "" // изначально пусто
                });
            }

            Recalculate(Difficulty.Bachelor);
        }

        public void EnsurePresetIfBachelor(Difficulty level, string? segmentName)
        {
            if (level == Difficulty.Bachelor && Rows.Count == 0)
                ApplyPresetForBachelor(segmentName);
        }

        public void AddRow()
        {
            var idx = Rows.Count == 0 ? 0 : Rows.Max(r => r.Index) + 1;
            Rows.Add(new FactorRow
            {
                Index = idx,
                Name = "",
                WeightText = ""
            });
        }

        public void RemoveRow(FactorRow row)
        {
            if (Rows.Contains(row))
            {
                Rows.Remove(row);
                Reindex();
                Recalculate(_lastLevel);
            }
        }

        public void Recalculate(Difficulty level)
        {
            _lastLevel = level;   // запомним, чтобы внутренние вызовы не слетали в Master
            AttachRowHandlers();

            double s = 0;
            bool allWeightsValid = true;

            foreach (var r in Rows)
            {
                if (FactorRow.TryParseWeight(r.WeightText, out var v))
                    s += v;
                else if (!string.IsNullOrWhiteSpace(r.WeightText))
                    allWeightsValid = false;
            }

            s = Math.Round(s, 2, MidpointRounding.AwayFromZero);
            Sum = s;

            bool namesOk = Rows.Count > 0 && Rows.All(r => !string.IsNullOrWhiteSpace(r.Name));
            bool countOk = Rows.Count >= ((level == Difficulty.Bachelor || level == Difficulty.Developer) ? 10 : 2);
            bool sumOk = Math.Abs(s - 1.0) <= 0.005;

            IsValid = namesOk && countOk && sumOk && allWeightsValid;
        }

        public void Reset()
        {
            Rows.Clear();
            IsValid = false;
            Sum = 0;
        }

        private void Rows_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (FactorRow r in e.OldItems) r.PropertyChanged -= Row_PropertyChanged;

            if (e.NewItems != null)
                foreach (FactorRow r in e.NewItems) r.PropertyChanged += Row_PropertyChanged;
        }

        private void AttachRowHandlers()
        {
            foreach (var r in Rows) r.PropertyChanged -= Row_PropertyChanged;
            foreach (var r in Rows) r.PropertyChanged += Row_PropertyChanged;
        }

        private void Row_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FactorRow.Name) || e.PropertyName == nameof(FactorRow.WeightText))
            {
                Recalculate(_lastLevel);
            }
        }

        private void Reindex()
        {
            int i = 0;
            foreach (var r in Rows) r.Index = i++;
        }

        public void ApplyPresetDeveloper()
        {
            Rows.Clear();

            void Add(string name, double w)
            {
                var row = new FactorRow
                {
                    Name = name
                };

                // ВЕС ТОЛЬКО ЧЕРЕЗ ТЕКСТ, чтобы модель сама посчитала WeightValue
                try { row.WeightText = w.ToString("0.00"); } catch { /* ok */ }

                Rows.Add(row);
            }

            Add("Соответствие реестру Минцифры", 0.20);
            Add("Совместимость с legacy-системами", 0.15);
            Add("Стоимость лицензии/подписки", 0.15);
            Add("Локализация и хранение данных (ФЗ-152)", 0.10);
            Add("Качество техподдержки (SLA)", 0.10);
            Add("Безопасность/сертификация ФСТЭК", 0.10);
            Add("Удобство интерфейса (UX)", 0.10);
            Add("API и интеграции (1С, Госуслуги)", 0.08);
            Add("Масштабируемость/производительность", 0.00);
            Add("Открытый код/исходники (импортозамещение)", 0.02);
        }
    }
}
