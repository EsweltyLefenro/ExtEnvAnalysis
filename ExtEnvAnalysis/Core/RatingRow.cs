using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace ExtEnvAnalysis.Core
{
    public partial class RatingRow : ObservableObject
    {
        public RatingRow(FactorRow factor)
        {
            Factor = factor;
            Factor.PropertyChanged += Factor_PropertyChanged;
        }

        [ObservableProperty] private FactorRow factor;

        [ObservableProperty] private string? myText;
        [ObservableProperty] private string? aText;
        [ObservableProperty] private string? bText;
        [ObservableProperty] private string? cText;

        public int MyValue => ParseScore(MyText);
        public int AValue => ParseScore(AText);
        public int BValue => ParseScore(BText);
        public int CValue => ParseScore(CText);

        public bool IsActive => (Factor?.WeightValue ?? 0) > 0 &&
                                !string.IsNullOrWhiteSpace(Factor?.Name);

        static int ParseScore(string? t)
        {
            if (string.IsNullOrWhiteSpace(t)) return 0;
            if (!int.TryParse(t.Trim(), out var v)) return 0;
            return (v >= 1 && v <= 10) ? v : 0;
        }

        private void Factor_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FactorRow.WeightText) ||
                e.PropertyName == nameof(FactorRow.Name))
            {
                OnPropertyChanged(nameof(IsActive));
            }
        }
    }
}
