using CommunityToolkit.Mvvm.ComponentModel;
using ExtEnvAnalysis.Core;

namespace ExtEnvAnalysis.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public AppState App { get; } = new();

    public bool CanTab1 => true;
    public bool CanTab2 => App.IsDeveloper || App.Segment.IsValid;
    public bool CanTab3 => App.IsDeveloper || (App.Segment.IsValid && App.Pestel.IsValid);
    public bool CanTab4 => App.IsDeveloper || (App.Segment.IsValid && App.Pestel.IsValid && App.Factors.IsValid && App.Factors.ActiveCount >= 3);
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
                OnPropertyChanged(nameof(CanTab5));
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
        try { App.Factors?.Recalculate(App.Profile.Difficulty); } catch { }
        try { App.Ratings?.Recalculate(); } catch { }

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
