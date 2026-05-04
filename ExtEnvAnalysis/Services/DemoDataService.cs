using ExtEnvAnalysis.Core;
using System;
using System.Linq;

namespace ExtEnvAnalysis.Services;

public static class DemoDataService
{
    public static void ApplyDeveloperPreset(AppState app)
    {
        if (string.IsNullOrWhiteSpace(app.Profile.FullName))
            app.Profile.FullName = "Александров С.А.";

        if (string.IsNullOrWhiteSpace(app.Profile.Group))
            app.Profile.Group = "Я преподаватель";

        if (string.IsNullOrWhiteSpace(app.Segment.SegmentName))
            app.Segment.SegmentName = "Разработка программного обеспечения";

        foreach (var category in app.Pestel.Categories)
        {
            var examples = GetPestelExamples(category.Type);
            if (string.IsNullOrWhiteSpace(category.Field1)) category.Field1 = examples.ElementAtOrDefault(0) ?? "Фактор 1";
            if (string.IsNullOrWhiteSpace(category.Field2)) category.Field2 = examples.ElementAtOrDefault(1) ?? "Фактор 2";
            if (string.IsNullOrWhiteSpace(category.Field3)) category.Field3 = examples.ElementAtOrDefault(2) ?? "Фактор 3";
        }

        app.Pestel.Recalculate();
        app.Factors.ApplyPresetDeveloper();
        app.Ratings.AttachToFactors(app.Factors);
        ApplyRatingPreset(app.Ratings);

        app.Factors.Recalculate(app.Profile.Difficulty);
        app.Ratings.Recalculate();
        app.Comparisons.Rebuild(app.Factors, app.Ratings);

        foreach (var map in app.Comparisons.Maps)
        {
            if (string.IsNullOrWhiteSpace(map.Direction))
                map.Direction = "-";
        }

        if (string.IsNullOrWhiteSpace(app.Report.Conclusion))
            app.Report.Conclusion = "По результатам сравнения определены ключевые зоны развития и приоритеты улучшения конкурентной позиции компании.";
    }

    private static void ApplyRatingPreset(RatingsState ratings)
    {
        if (string.IsNullOrWhiteSpace(ratings.MarketMyText)) ratings.MarketMyText = "20";
        if (string.IsNullOrWhiteSpace(ratings.MarketAText)) ratings.MarketAText = "30";
        if (string.IsNullOrWhiteSpace(ratings.MarketBText)) ratings.MarketBText = "25";
        if (string.IsNullOrWhiteSpace(ratings.MarketCText)) ratings.MarketCText = "25";

        var rnd = new Random(Environment.TickCount);
        string Next() => rnd.Next(1, 11).ToString();

        foreach (var row in ratings.Rows)
        {
            if (string.IsNullOrWhiteSpace(row.MyText)) row.MyText = Next();
            if (string.IsNullOrWhiteSpace(row.AText)) row.AText = Next();
            if (string.IsNullOrWhiteSpace(row.BText)) row.BText = Next();
            if (string.IsNullOrWhiteSpace(row.CText)) row.CText = Next();
        }
    }

    private static string[] GetPestelExamples(PestelType type)
    {
        return type switch
        {
            PestelType.P => new[] { "Рост налоговой нагрузки", "Жестче правила госзакупок", "Нужны отраслевые лицензии" },
            PestelType.E => new[] { "Ускорение инфляции", "Рост курса валют", "Дефицит ИТ-кадров" },
            PestelType.S => new[] { "Спрос на удаленный доступ", "Рост цифровой грамотности", "Запрос на быстрый сервис" },
            PestelType.T => new[] { "Внедрение ИИ-сервисов", "Рост угроз кибератак", "Переход в облачные среды" },
            PestelType.Env => new[] { "Снижение энергозатрат", "Учет углеродного следа", "Экоутилизация техники" },
            PestelType.L => new[] { "Ужесточение ФЗ-152", "Риски по авторским правам", "Новые требования ГОСТ" },
            _ => new[] { "Фактор 1", "Фактор 2", "Фактор 3" }
        };
    }
}
