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
        catch { /* ok */ }

        // Если хочешь активировать пресет уже при старте,
        // когда проект загружается с выбранным Developer:
        try
        {
            if (Profile?.Difficulty == Difficulty.Developer)
                ApplyDeveloperPreset();
        }
        catch { /* ok */ }

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
                        try { Comparisons.Rebuild(Factors, Ratings); } catch { /* ok */ }
                        try { RulesChanged?.Invoke(); } catch { /* ok */ }
                    }
                };
            }
        }
        catch { /* ok */ }
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
        Ratings.AttachToFactors(Factors);
        Ratings.Recalculate();
        Comparisons.Rebuild(Factors, Ratings);

        RulesChanged?.Invoke();
    }

    // ===== ВСТАВЬ ВМЕСТО ТЕКУЩЕГО В AppState.cs =====
    public List<string> GetBlockingErrorsForReport()
    {
        var err = new List<string>();

        if (!Segment.IsValid)
            err.Add("Сегмент: выберите сегмент.");

        if (!Pestel.IsValid)
            err.Add("PESTEL: в каждой категории должно быть ≥ 3 пункта.");

        if (!Factors.IsValid)
            err.Add($"Факторы: сумма весов должна быть 1.00 (сейчас {Factors.Sum:0.00}); названия факторов не должны быть пустыми.");

        // Активные строки для оценок (вес > 0 и задано имя фактора)
        var active = Ratings.Rows.Where(r =>
            r?.Factor != null &&
            !string.IsNullOrWhiteSpace(r.Factor.Name) &&
            r.Factor.WeightValue > 0).ToList();

        if (active.Count == 0)
            err.Add("Оценка компаний: нет активных факторов (вес > 0).");

        // Баллы 0..10 должны парситься — используй твою сигнатуру TryScore(string?, out int)
        foreach (var r in active)
        {
            bool okMy = RatingsState.TryScore(r.MyText, out _);
            bool okA = RatingsState.TryScore(r.AText, out _);
            bool okB = RatingsState.TryScore(r.BText, out _);
            bool okC = RatingsState.TryScore(r.CText, out _);
            if (!(okMy && okA && okB && okC))
            {
                err.Add($"Оценки: проверьте строку «{r.Factor.Name}» (баллы 0..10).");
                break;
            }
        }
        return err;
    }

    // === ДОБАВЬ в класс AppState ===
    public void ApplyDeveloperPreset()
    {
        // 1) Профиль
        if (Profile != null)
        {
            if (string.IsNullOrWhiteSpace(Profile.FullName))
                Profile.FullName = "Александров С.А.";
            if (string.IsNullOrWhiteSpace(Profile.Group))
                Profile.Group = "Я преподаватель";
        }

        // 2) Сегмент
        if (Segment != null && string.IsNullOrWhiteSpace(Segment.SegmentName))
            Segment.SegmentName = "Разработка программного обеспечения";

        //// 3) PESTEL — заполнить пустые ячейки дефисом
        //try
        //{
        //    var cats = Pestel?.Categories;
        //    if (cats != null)
        //    {
        //        foreach (var c in cats)
        //        {
        //            if (string.IsNullOrWhiteSpace(c.Field1)) c.Field1 = "-";
        //            if (string.IsNullOrWhiteSpace(c.Field2)) c.Field2 = "-";
        //            if (string.IsNullOrWhiteSpace(c.Field3)) c.Field3 = "-";
        //            if (string.IsNullOrWhiteSpace(c.Field4)) c.Field4 = "-";
        //            if (string.IsNullOrWhiteSpace(c.Field5)) c.Field5 = "-";
        //        }
        //    }
        //}
        //catch { /* ok */ }

        // 4) Факторы — пресет «Разработчик»
        try { Factors?.ApplyPresetDeveloper(); } catch { /* ok */ }

        // 4.1) Оценки — автозаполнение вкладки 4 (только пустые поля)
        try { Ratings?.ApplyPresetDeveloper(); } catch { /* ok */ }

        // 5) Синхронизация зависимых разделов
        try { Factors?.Recalculate(Profile.Difficulty); } catch { /* ok */ }
        try { Ratings?.Recalculate(); } catch { /* ok */ }
        try { Comparisons?.Rebuild(Factors, Ratings); } catch { /* ok */ }
    }

}
