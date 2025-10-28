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
    public bool CanTab5 => App.IsDeveloper || (CanTab4 && App.Ratings.IsValid);
    public bool CanTab6 => App.IsDeveloper || (CanTab5 && App.Comparisons?.Maps != null && App.Comparisons.Maps.All(m => !string.IsNullOrWhiteSpace(m.Direction)));
    public bool CanTab7 => App.IsDeveloper || !string.IsNullOrWhiteSpace(App?.Report?.Conclusion);

    private int _selectedTabIndex;

    public bool IsBachelorLike => App?.Profile?.Difficulty is Difficulty.Bachelor or Difficulty.Developer;

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    public void NotifyRulesChanged()
    {
        // 1) пересчёты (если методы есть — выполнятся; если нет — тихо пропустим)
        try { App.Factors?.Recalculate(App.Profile.Difficulty); } catch { }
        try { App.Ratings?.Recalculate(); } catch { }
        // try { App.Comparisons?.Rebuild(App.Factors, App.Ratings); } catch { }

        // 2) обновить биндинги доступности вкладок
        OnPropertyChanged(nameof(CanTab1));
        OnPropertyChanged(nameof(CanTab2));
        OnPropertyChanged(nameof(CanTab3));
        OnPropertyChanged(nameof(CanTab4));
        OnPropertyChanged(nameof(CanTab5));
        OnPropertyChanged(nameof(CanTab6));
        OnPropertyChanged(nameof(CanTab7));

        OnPropertyChanged(nameof(IsBachelorLike));
    }
}
