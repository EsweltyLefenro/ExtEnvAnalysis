using CommunityToolkit.Mvvm.ComponentModel;
using ExtEnvAnalysis.Services;
using System;

namespace ExtEnvAnalysis.Core;

public partial class AppState : ObservableObject
{
    public ProfileState Profile { get; } = new();
    public SegmentState Segment { get; } = new();
    public PestelState Pestel { get; } = new();
    public TargetAudienceState TargetAudience { get; } = new();
    public FactorsState Factors { get; } = new();
    public RatingsState Ratings { get; } = new();
    public ComparisonsState Comparisons { get; } = new();
    public ReportState Report { get; } = new();

    public bool IsDeveloper => Profile.IsDeveloper;

    public ValidationService Validation { get; } = new();
    public void RevalidateAll() => Validation.Rebuild(this);

    public AppState()
    {
        Factors.InitForDifficulty(Profile.Difficulty);
        Ratings.AttachToFactors(Factors);
        Report ??= new ReportState();

        try
        {
            if (Profile != null)
                Profile.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(Profile.Difficulty) && Profile.Difficulty == Difficulty.Developer)
                        ApplyDeveloperPreset();
                };
        }
        catch {  }

        try
        {
            if (Profile?.Difficulty == Difficulty.Developer)
                ApplyDeveloperPreset();
        }
        catch {  }

        try
        {
            if (Ratings != null)
            {
                Ratings.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(RatingsState.CompanyAName) ||
                        e.PropertyName == nameof(RatingsState.CompanyBName) ||
                        e.PropertyName == nameof(RatingsState.CompanyCName))
                    {
                        try { Comparisons.Rebuild(Factors, Ratings); } catch {  }
                        try { RulesChanged?.Invoke(); } catch {  }
                    }
                };
            }
        }
        catch {  }
    }

    private void Reset(params ISection[] sections)
    {
        foreach (var s in sections) s.Reset();
    }

    private void ResetAllExcept(params ISection[] keep)
    {
        var all = new ISection[] { Segment, Pestel, TargetAudience, Factors, Ratings, Comparisons, Report };
        foreach (var s in all)
            if (Array.Find(keep, k => ReferenceEquals(k, s)) is null)
                s.Reset();
    }

    public void ResetAllExceptProfile()
    {
        ResetAllExcept();
        Ratings.Reset();
        Ratings.AttachToFactors(Factors);
    }

    public void OnDifficultyChanged()
    {
        if (Profile.Difficulty == Difficulty.Bachelor)
            Factors.ApplyPresetForBachelor(Segment.SegmentName);

    }

    public event Action? RulesChanged;

    private void OnRulesChanged() => RulesChanged?.Invoke();

    public void RatingsChanged()
    {
        Ratings.Recalculate();
        Comparisons.Rebuild(Factors, Ratings);
        RulesChanged?.Invoke();
    }

    public void FactorsChanged()
    {
        Factors.Recalculate(Profile.Difficulty);
        Ratings.SyncWithFactors(Factors);
        Ratings.Recalculate();
        Comparisons.Rebuild(Factors, Ratings);
        RulesChanged?.Invoke();
    }

    public void ChangeSegment(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;

        Segment.SegmentName = name;

        TargetAudience.Reset();
        Factors.Reset();
        Ratings.Reset();
        Comparisons.Reset();
        Report.Reset();

        if (Profile.Difficulty is Difficulty.Bachelor or Difficulty.Developer)
            Factors.ApplyPresetForBachelor(name);

        Ratings.AttachToFactors(Factors);
        Ratings.Recalculate();

        RulesChanged?.Invoke();
    }

    public List<string> GetBlockingErrorsForReport()
    {
        return Validation.GetBlockingErrorsForReport(this);
    }

    public void ApplyDeveloperPreset()
    {
        DemoDataService.ApplyDeveloperPreset(this);
    }

}
