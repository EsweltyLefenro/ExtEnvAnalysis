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
    public bool CanTab4 => App.IsDeveloper || (App.Segment.IsValid && App.Pestel.IsValid && App.Factors.IsValid && App.Factors.ActiveCount >= 3);    // 4. Факторы/веса
    public bool CanTab5 => App.IsDeveloper || (CanTab4 && App.Ratings.IsValid);
    public bool CanTab6 => App.IsDeveloper || (CanTab5 && App.Comparisons?.Maps != null && App.Comparisons.Maps.All(m => !string.IsNullOrWhiteSpace(m.Direction)));
    public bool CanTab7 => App.IsDeveloper || (CanTab6 && !string.IsNullOrWhiteSpace(App?.Report?.Conclusion));

    private int _selectedTabIndex;

    public bool IsBachelorLike => App?.Profile?.Difficulty is Difficulty.Bachelor or Difficulty.Developer;

    public MainViewModel()
    {
        App.Report.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(App.Report.Conclusion))
                OnPropertyChanged(nameof(CanTab7));
        };
        App.Factors.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(App.Factors.IsValid) ||
                e.PropertyName == nameof(App.Factors.ActiveCount) ||
                e.PropertyName == nameof(App.Factors.Sum))
            {
                OnPropertyChanged(nameof(CanTab4));
                OnPropertyChanged(nameof(CanTab5)); // на всякий случай, цепочки
                OnPropertyChanged(nameof(CanTab6));
            }
        };
    }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (SetProperty(ref _selectedTabIndex, value))
            {
                // 4-я вкладка: Оценка компаний (индекс 3) — оставляем твой код
                if (value == 3)
                {
                    try
                    {
                        if (App.Factors is not null && App.Ratings is not null)
                        {
                            App.Factors.Recalculate(App.Profile.Difficulty);
                            App.Ratings.SyncWithFactors(App.Factors);
                            App.Ratings.Recalculate();
                        }
                    }
                    catch { }

                    OnPropertyChanged(nameof(CanTab4));
                    OnPropertyChanged(nameof(CanTab5));
                    OnPropertyChanged(nameof(CanTab6));
                    OnPropertyChanged(nameof(CanTab7));
                }

                // 5-я вкладка: Карты (индекс 4) — строим карты,
                // только если оценки валидны (иначе просто не мешаемся)
                if (value == 4)
                {
                    try
                    {
                        if (App.Factors is not null && App.Ratings?.IsValid == true)
                            App.Comparisons?.Rebuild(App.Factors, App.Ratings);
                    }
                    catch { }

                    OnPropertyChanged(nameof(CanTab6));
                    OnPropertyChanged(nameof(CanTab7));
                }
            }
        }
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
