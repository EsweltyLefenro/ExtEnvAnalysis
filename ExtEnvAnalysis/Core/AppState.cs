using CommunityToolkit.Mvvm.ComponentModel;
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

    public AppState()
    {
        // Factors уже создан (у тебя, скорее всего, стоит = new(); при объявлении)
        Factors.InitForDifficulty(Profile.Difficulty);
        Ratings.AttachToFactors(Factors);
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
        // Сбрасываем все секции; профиль не трогаем (его нет в списке all)
        ResetAllExcept();
        Ratings.Reset();
        Ratings.AttachToFactors(Factors);
    }

    public void OnDifficultyChanged()
    {
        if (Profile.Difficulty == Difficulty.Bachelor)
            Factors.ApplyPresetForBachelor(Segment.SegmentName);

        // если у тебя логика «при смене уровня всё сбрасывать» — оставь её тоже
        // ResetAllExceptProfile(); …
    }

    public event Action? RulesChanged;

    private void OnRulesChanged() => RulesChanged?.Invoke();

    public void RatingsChanged()
    {
        Ratings.Recalculate();                 // ← у тебя такой метод есть в RatingsState
        Comparisons.Rebuild(Factors, Ratings); // ← после перерасчёта оценок пересобираем карты
        RulesChanged?.Invoke();                // ← вместо несуществующего OnRulesChanged()
    }

    public void FactorsChanged()
    {
        Factors.Recalculate(Profile.Difficulty);
        Ratings.Recalculate();                 // безопасно «освежить» итоги
        Comparisons.Rebuild(Factors, Ratings);
        RulesChanged?.Invoke();
    }

    public void ChangeSegment(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;

        Segment.SegmentName = name;

        if (Profile.Difficulty == Difficulty.Bachelor)
            Factors.ApplyPresetForBachelor(name);

        Ratings.Reset();
        Ratings.AttachToFactors(Factors);
        Comparisons.Reset();
        Report.Reset();

        RulesChanged?.Invoke();
    }
}
