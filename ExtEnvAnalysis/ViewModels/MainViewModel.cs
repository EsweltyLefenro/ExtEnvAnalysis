using CommunityToolkit.Mvvm.ComponentModel;
using ExtEnvAnalysis.Core;

namespace ExtEnvAnalysis.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public AppState App { get; } = new();

    // Доступ к вкладкам (обычный режим — по порядку, dev — свободно)
    public bool CanTab1 => true; // 1. Сегмент
    public bool CanTab2 => App.IsDeveloper || App.Segment.IsValid; // 2. PESTEL
    public bool CanTab3 => App.IsDeveloper || (App.Segment.IsValid && App.Pestel.IsValid); // 3. ЦА
    public bool CanTab4 => App.IsDeveloper || (App.Segment.IsValid && App.Pestel.IsValid && App.Factors.IsValid); // 4. Факторы/веса
    public bool CanTab5 => App.IsDeveloper || (CanTab4 && App.Ratings.IsValid);        // 5. Оценка компаний
                                                                                       // 1..5 пусть останутся как были у тебя
    public bool CanTab6 => App.IsDeveloper || (CanTab5 && App.Comparisons?.Maps != null && App.Comparisons.Maps.All(m => !string.IsNullOrWhiteSpace(m.Direction)));

    public bool CanTab7 => CanTab6; // 7 включается вместе с 6


    public void NotifyRulesChanged()
    {
        OnPropertyChanged(nameof(CanTab1));
        OnPropertyChanged(nameof(CanTab2));
        OnPropertyChanged(nameof(CanTab3));
        OnPropertyChanged(nameof(CanTab4));
        OnPropertyChanged(nameof(CanTab5));
        OnPropertyChanged(nameof(CanTab6));
        OnPropertyChanged(nameof(CanTab7));
    }

    private int _selectedTabIndex;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }
}
