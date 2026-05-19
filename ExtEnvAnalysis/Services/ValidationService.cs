using CommunityToolkit.Mvvm.ComponentModel;
using ExtEnvAnalysis.Core;
using System.Collections.ObjectModel;

namespace ExtEnvAnalysis.Services;

public enum CheckSeverity { Info, Warning, Error }

public record CheckItem(CheckSeverity Severity, string Message, string? SectionKey = null)
{
    public bool IsBlocking => Severity == CheckSeverity.Error;
}

public class ValidationService : ObservableObject
{
    public ObservableCollection<CheckItem> Items { get; } = new();

    public bool HasBlocking => Items.Any(i => i.IsBlocking);

    public List<string> GetBlockingErrorsForReport(AppState app)
    {
        var err = new List<string>();

        if (string.IsNullOrWhiteSpace(app.Profile.FullName) || string.IsNullOrWhiteSpace(app.Profile.Group))
            err.Add("Профиль: заполните фамилию, имя и группу.");

        if (!app.Segment.IsValid)
            err.Add("Сегмент: выберите сегмент.");

        if (!app.Pestel.IsValid)
            err.Add("PESTEL: в каждой категории должно быть ≥ 3 пункта.");

        if (!app.Factors.IsValid)
            err.Add($"Факторы: сумма весов должна быть 1.00 (сейчас {app.Factors.Sum:0.00}); названия факторов не должны быть пустыми.");

        var active = app.Ratings.Rows.Where(r =>
            r?.Factor != null &&
            !string.IsNullOrWhiteSpace(r.Factor.Name) &&
            r.Factor.WeightValue > 0).ToList();

        if (active.Count == 0)
            err.Add("Оценка компаний: нет активных факторов (вес > 0).");
        else if (active.Count < 3)
            err.Add("Оценка компаний: выберите не менее 3 активных факторов.");

        foreach (var row in active)
        {
            bool okMy = RatingsState.TryScore(row.MyText, out _);
            bool okA = RatingsState.TryScore(row.AText, out _);
            bool okB = RatingsState.TryScore(row.BText, out _);
            bool okC = RatingsState.TryScore(row.CText, out _);
            if (!(okMy && okA && okB && okC))
            {
                err.Add($"Оценки: проверьте строку «{row.Factor.Name}» (баллы 1..10).");
                break;
            }
        }

        if (!app.Ratings.AreMarketSharesValid())
            err.Add("Доли рынка: значения должны быть от 0 до 100, а их сумма — не более 100%.");

        if (app.Comparisons.Maps.Count == 0)
            err.Add("Карты сравнения: сформируйте стратегические карты.");
        else if (app.Comparisons.Maps.Any(map => string.IsNullOrWhiteSpace(map.Direction)))
            err.Add("Карты сравнения: заполните направление развития для каждой карты.");

        if (string.IsNullOrWhiteSpace(app.Report.Conclusion))
            err.Add("Итоги: введите итоговые выводы.");

        return err;
    }

    public void Rebuild(AppState app)
    {
        Items.Clear();

        if (string.IsNullOrWhiteSpace(app.Profile.FullName) || string.IsNullOrWhiteSpace(app.Profile.Group))
            Items.Add(new CheckItem(CheckSeverity.Error, "Профиль: заполните фамилию, имя и группу", "PROFILE"));

        if (!app.Segment.IsValid)
            Items.Add(new CheckItem(CheckSeverity.Error, "Сегмент: выберите сегмент", "SEGMENT"));

        // PESTEL: в каждой букве >= 3 пункта
        foreach (var type in new[] { PestelType.P, PestelType.E, PestelType.S, PestelType.T, PestelType.Env, PestelType.L })
        {
            var cnt = app.Pestel?.Categories.FirstOrDefault(c => c.Type == type)?
                .Let(c => new[] { c.Field1, c.Field2, c.Field3, c.Field4, c.Field5 }.Count(s => !string.IsNullOrWhiteSpace(s))) ?? 0;
            if (cnt < 3) Items.Add(new CheckItem(CheckSeverity.Error, $"PESTEL: раздел {type} должен содержать минимум 3 пункта", "PESTEL"));
        }

        // Факторы: имена и сумма весов
        var activeFactors = app.Factors.Rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Name) && r.WeightValue > 0)
            .ToList();
        if (activeFactors.Count < 3)
            Items.Add(new CheckItem(CheckSeverity.Error, "Выберите не менее 3 активных факторов", "FACTORS"));

        if (Math.Abs(app.Factors.Sum - 1.0) > 1e-6)
            Items.Add(new CheckItem(CheckSeverity.Error, $"Сумма весов = {app.Factors.Sum:0.00}, должна быть 1.00", "FACTORS"));

        // Оценки: по активным факторам должны быть баллы 1..10.
        var ratingActive = app.Ratings.Rows.Where(r => r?.Factor != null && r.Factor.WeightValue > 0).ToList();
        if (ratingActive.Count == 0)
            Items.Add(new CheckItem(CheckSeverity.Error, "Нет активных факторов для оценок", "RATINGS"));
        else if (ratingActive.Any(r =>
            !RatingsState.TryScore(r.MyText, out _) ||
            !RatingsState.TryScore(r.AText, out _) ||
            !RatingsState.TryScore(r.BText, out _) ||
            !RatingsState.TryScore(r.CText, out _)))
        {
            Items.Add(new CheckItem(CheckSeverity.Error, "Проверьте оценки компаний: все баллы должны быть от 1 до 10", "RATINGS"));
        }

        if (!app.Ratings.AreMarketSharesValid())
            Items.Add(new CheckItem(CheckSeverity.Error, "Доли рынка должны быть от 0 до 100, сумма — не более 100%", "RATINGS"));

        if (app.Comparisons.Maps.Count == 0)
            Items.Add(new CheckItem(CheckSeverity.Error, "Сформируйте стратегические карты", "MAPS"));
        else if (app.Comparisons.Maps.Any(m => string.IsNullOrWhiteSpace(m.Direction)))
            Items.Add(new CheckItem(CheckSeverity.Error, "Заполните направления развития для всех карт", "MAPS"));

        if (string.IsNullOrWhiteSpace(app.Report.Conclusion))
            Items.Add(new CheckItem(CheckSeverity.Error, "Введите итоговые выводы", "REPORT"));
    }
}

// маленький хелпер
static class _Ext
{
    public static TOut Let<TIn, TOut>(this TIn? v, Func<TIn, TOut> f) where TIn : class
    => v is null ? default! : f(v);
}
