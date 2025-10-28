using CommunityToolkit.Mvvm.ComponentModel;
using ExtEnvAnalysis.Core;
using System.Collections.ObjectModel;

public enum CheckSeverity { Info, Warning, Error }

public record CheckItem(CheckSeverity Severity, string Message, string? SectionKey = null)
{
    public bool IsBlocking => Severity == CheckSeverity.Error;
}

public class ValidationService : ObservableObject
{
    public ObservableCollection<CheckItem> Items { get; } = new();

    public bool HasBlocking => Items.Any(i => i.IsBlocking);

    public void Rebuild(AppState app)
    {
        Items.Clear();

        // PESTEL: в каждой букве >= 1 пункта
        foreach (var type in new[] { PestelType.P, PestelType.E, PestelType.S, PestelType.T, PestelType.Env, PestelType.L })
        {
            var cnt = app.Pestel?.Categories.FirstOrDefault(c => c.Type == type)?
                .Let(c => new[] { c.Field1, c.Field2, c.Field3, c.Field4, c.Field5 }.Count(s => !string.IsNullOrWhiteSpace(s))) ?? 0;
            if (cnt == 0) Items.Add(new CheckItem(CheckSeverity.Error, $"PESTEL: раздел {type} пуст", "PESTEL"));
        }

        // Факторы: имена и сумма весов
        var activeFactors = app.Factors?.Rows?.Where(r => !string.IsNullOrWhiteSpace(r.Name)).ToList() ?? new();
        if (activeFactors.Count == 0)
            Items.Add(new CheckItem(CheckSeverity.Error, "Не задан ни один фактор", "FACTORS"));

        if (Math.Abs(app.Factors.Sum - 1.0) > 1e-6)
            Items.Add(new CheckItem(CheckSeverity.Error, $"Сумма весов = {app.Factors.Sum:0.00}, должна быть 1.00", "FACTORS"));

        // Оценки: по активным факторам должны быть баллы (допустим 0..10)
        var ratingActive = app.Ratings?.Rows?.Where(r => r?.Factor != null && r.Factor.WeightValue > 0).ToList() ?? new();
        if (ratingActive.Count == 0)
            Items.Add(new CheckItem(CheckSeverity.Error, "Нет активных факторов для оценок", "RATINGS"));
    }
}

// маленький хелпер
static class _Ext
{
    public static TOut Let<TIn, TOut>(this TIn? v, Func<TIn, TOut> f) where TIn : class
    => v is null ? default! : f(v);
}
