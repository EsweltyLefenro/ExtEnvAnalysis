using ExtEnvAnalysis;               // AppState
using ExtEnvAnalysis.Controls;      // StrategyMap
using ExtEnvAnalysis.Core;
using ExtEnvAnalysis.Models;        // MapModel
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ExtEnvAnalysis.Services
{
    public static class ReportBuilder
    {
        public static string BuildPdf(AppState app)
        {
            var file = Path.Combine(Path.GetTempPath(),
                $"ExtEnvAnalysis_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(11));
                    page.Header().AlignCenter().Text("Анализ внешнего рынка").Bold().FontSize(18);

                    page.Content().Column(col =>
                    {
                        // 1) Профиль
                        col.Item().PaddingBottom(10).Element(e =>
                            e.Column(c =>
                            {
                                c.Item().Text(x => x.Span("Профиль").Bold().FontSize(14));
                                c.Item().Text($"ФИО: {app?.Profile?.FullName ?? ""}");
                                c.Item().Text($"Группа: {app?.Profile?.Group ?? ""}");
                                c.Item().Text($"Уровень: {app?.Profile?.Difficulty}");
                            }));

                        // 2) Сегмент
                        col.Item().PaddingBottom(10).Element(e =>
                            e.Column(c =>
                            {
                                c.Item().Text(x => x.Span("Сегмент").Bold().FontSize(14));
                                var seg = app?.Segment?.SegmentName ?? "";
                                c.Item().Text(string.IsNullOrWhiteSpace(seg) ? "—" : seg);
                            }));

                        // 3) PESTEL
                        col.Item().PaddingBottom(10).Element(e =>
                            e.Column(c =>
                            {
                                c.Item().Text(x => x.Span("PESTEL").Bold().FontSize(14));
                                PrintPestelList(c, "P — Политические", app?.Pestel, PestelType.P);
                                PrintPestelList(c, "E — Экономические", app?.Pestel, PestelType.E);
                                PrintPestelList(c, "S — Социальные", app?.Pestel, PestelType.S);
                                PrintPestelList(c, "T — Технологические", app?.Pestel, PestelType.T);
                                PrintPestelList(c, "E — Экологические", app?.Pestel, PestelType.Env);
                                PrintPestelList(c, "L — Правовые", app?.Pestel, PestelType.L);
                            }));

                        // --- Разрыв страницы перед "Факторы и веса"
                        col.Item().PageBreak();

                        // 4) Факторы и веса (только активные: WeightValue > 0)
                        col.Item().PaddingBottom(10).Element(e =>
                            e.Column(c =>
                            {
                                c.Item().Text(x => x.Span("Факторы и веса").Bold().FontSize(14));

                                var rows = app?.Factors?.Rows;
                                if (rows == null || rows.Count == 0)
                                {
                                    c.Item().Text("—");
                                }
                                else
                                {
                                    var active = rows
                                        .Where(r => !string.IsNullOrWhiteSpace(r.Name) && (r.WeightValue > 0))
                                        .ToList();

                                    if (active.Count == 0)
                                    {
                                        c.Item().Text("—");
                                    }
                                    else
                                    {
                                        c.Item().Table(t =>
                                        {
                                            t.ColumnsDefinition(cd =>
                                            {
                                                cd.RelativeColumn(3); // Фактор
                                                cd.RelativeColumn(1); // Вес (текст)
                                            });

                                            t.Header(h =>
                                            {
                                                h.Cell().PaddingBottom(4).Text("Фактор").Bold();
                                                h.Cell().PaddingBottom(4).AlignRight().Text("Вес").Bold();
                                            });

                                            foreach (var r in active)
                                            {
                                                var weightText = string.IsNullOrWhiteSpace(r.WeightText)
                                                    ? r.WeightValue.ToString("0.00")
                                                    : r.WeightText;

                                                t.Cell().PaddingVertical(2).Text(r.Name ?? "");
                                                t.Cell().PaddingVertical(2).AlignRight().Text(weightText);
                                            }
                                        });

                                        c.Item().Text($"Сумма весов: {app.Factors.Sum:0.00}");
                                    }
                                }
                            }));

                        // 5) Оценка компаний (по активным строкам Ratings -> Factor.WeightValue > 0)
                        col.Item().PaddingBottom(10).Element(e =>
                            e.Column(c =>
                            {
                                c.Item().Text(x => x.Span("Оценка компаний").Bold().FontSize(14));

                                var rows = app?.Ratings?.Rows;
                                if (rows == null || rows.Count == 0)
                                {
                                    c.Item().Text("—");
                                    return;
                                }

                                var active = rows
                                    .Where(row => row?.Factor != null
                                                  && !string.IsNullOrWhiteSpace(row.Factor.Name)
                                                  && row.Factor.WeightValue > 0)
                                    .ToList();

                                if (active.Count == 0)
                                {
                                    c.Item().Text("—");
                                    return;
                                }

                                c.Item().Table(t =>
                                {
                                    t.ColumnsDefinition(cd =>
                                    {
                                        cd.RelativeColumn(4);  // Фактор
                                        cd.RelativeColumn(1);  // Мы
                                        cd.RelativeColumn(1);  // A
                                        cd.RelativeColumn(1);  // B
                                        cd.RelativeColumn(1);  // C
                                    });

                                    t.Header(h =>
                                    {
                                        h.Cell().PaddingBottom(4).Text("Фактор").Bold();
                                        h.Cell().PaddingBottom(4).AlignCenter().Text("Мы").Bold();
                                        h.Cell().PaddingBottom(4).AlignCenter().Text("A").Bold();
                                        h.Cell().PaddingBottom(4).AlignCenter().Text("B").Bold();
                                        h.Cell().PaddingBottom(4).AlignCenter().Text("C").Bold();
                                    });

                                    foreach (var row in active)
                                    {
                                        string F(int v) => v == 0 ? "" : v.ToString("0");
                                        t.Cell().PaddingVertical(2).Text(row.Factor.Name ?? "");
                                        t.Cell().AlignCenter().Text(F(row.MyValue));
                                        t.Cell().AlignCenter().Text(F(row.AValue));
                                        t.Cell().AlignCenter().Text(F(row.BValue));
                                        t.Cell().AlignCenter().Text(F(row.CValue));
                                    }
                                });
                            }));

                        // 6) Карты стратегии + пояснение + направление
                        var maps = app?.Comparisons?.Maps;
                        if (maps != null && maps.Any())
                        {
                            int i = 1;
                            foreach (var map in maps)
                            {
                                col.Item().PaddingTop(10).Text($"Стратегическая карта {i}").Bold().FontSize(14);

                                // Рендерим квадрат: ширину сохраняем (800), высоту делаем равной ширине.
                                var png = RenderMapToPng(map, 800, 800);
                                if (png != null)
                                {
                                    // Квадратное отображение в PDF (высота = ширина визуально).
                                    col.Item().Height(300).Image(png);
                                }

                                col.Item().PaddingTop(6).Text("Пояснение").Bold();
                                col.Item().Text(map.Explanation ?? "");

                                // Пустая строка перед "Направление развития"
                                col.Item().PaddingTop(6).Text("");
                                col.Item().Text("Направление развития").Bold();
                                col.Item().Text(string.IsNullOrWhiteSpace(map.Direction) ? (i.ToString()) : map.Direction);

                                i++;
                                col.Item().PageBreak();
                            }
                        }

                        // 7) Итоги (взвешенные, нормированные на сумму весов активных факторов)
                        col.Item().PaddingTop(10).Element(e =>
                            e.Column(c =>
                            {
                                c.Item().Text(x => x.Span("Итоги").Bold().FontSize(14));

                                var rrows = app?.Ratings?.Rows;
                                var factRows = app?.Factors?.Rows;

                                if (rrows == null || rrows.Count == 0 || factRows == null || factRows.Count == 0)
                                {
                                    c.Item().Text("—");
                                    return;
                                }

                                var active = rrows
                                    .Where(row => row?.Factor != null
                                                  && !string.IsNullOrWhiteSpace(row.Factor.Name)
                                                  && row.Factor.WeightValue > 0)
                                    .ToList();

                                if (active.Count == 0)
                                {
                                    c.Item().Text("—");
                                    return;
                                }

                                double sumW = active.Sum(x => x.Factor.WeightValue);

                                double Score(Func<Core.RatingRow, int> selector)
                                    => active.Sum(x => x.Factor.WeightValue * selector(x));

                                double my = Score(x => x.MyValue);
                                double a = Score(x => x.AValue);
                                double b = Score(x => x.BValue);
                                double c0 = Score(x => x.CValue);

                                // Нормировка на сумму весов (если сумма не равна 1)
                                double N(double s) => sumW > 0 ? s / sumW : 0.0;

                                double myN = N(my);
                                double aN = N(a);
                                double bN = N(b);
                                double cN = N(c0);

                                c.Item().Table(t =>
                                {
                                    t.ColumnsDefinition(cd =>
                                    {
                                        cd.RelativeColumn(2); // Компания
                                        cd.RelativeColumn(1); // Итог
                                    });

                                    t.Header(h =>
                                    {
                                        h.Cell().PaddingBottom(4).Text("Компания").Bold();
                                        h.Cell().PaddingBottom(4).AlignRight().Text("Итог (0–10)").Bold();
                                    });

                                    void Row(string name, double val)
                                    {
                                        t.Cell().PaddingVertical(2).Text(name);
                                        t.Cell().PaddingVertical(2).AlignRight().Text(val.ToString("0.00"));
                                    }

                                    Row("Мы", myN);
                                    Row("A", aN);
                                    Row("B", bN);
                                    Row("C", cN);
                                });

                                // Определяем лидера
                                var best = new[]
                                {
                                    ("Мы", myN),
                                    ("A",  aN),
                                    ("B",  bN),
                                    ("C",  cN)
                                }.OrderByDescending(x => x.Item2).ToList();

                                var top = best.First();
                                // Если есть равенство — явно отметим
                                var ties = best.Where(x => Math.Abs(x.Item2 - top.Item2) < 1e-9).Select(x => x.Item1).ToList();

                                if (ties.Count > 1)
                                    c.Item().PaddingTop(6).Text($"Лидеры: {string.Join(", ", ties)} (по {top.Item2:0.00}).");
                                else
                                    c.Item().PaddingTop(6).Text($"Лидер: {top.Item1} ({top.Item2:0.00}).");

                                // Пояснение про сумму весов
                                c.Item().Text($"Сумма активных весов: {sumW:0.00}.");
                            }));
                    });

                    page.Footer().AlignRight().Text(x =>
                    {
                        x.Span("Стр. ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            })
            .GeneratePdf(file);

            return file;
        }

        // ===== PESTEL helpers =====

        private static void PrintPestelList(ColumnDescriptor col, string title, PestelState? state, PestelType type)
        {
            col.Item().Text(x => x.Span(title).Bold());
            var lines = GetPestelLines(state, type);

            if (lines.Length == 0)
            {
                col.Item().Text("—");
                return;
            }

            foreach (var s in lines)
                col.Item().Text("• " + s);
        }

        private static string[] GetPestelLines(PestelState? state, PestelType type)
        {
            if (state == null) return Array.Empty<string>();

            var cat = state.Categories.FirstOrDefault(c => c.Type == type);
            if (cat == null) return Array.Empty<string>();

            string[] arr = new[]
            {
                cat.Field1, cat.Field2, cat.Field3, cat.Field4, cat.Field5
            }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim())
            .ToArray();

            return arr;
        }

        // ===== Map render helper =====

        private static byte[]? RenderMapToPng(MapModel map, int width, int height)
        {
            try
            {
                var ctrl = new StrategyMap { Width = width, Height = height, Map = map };
                ctrl.Measure(new System.Windows.Size(width, height));
                ctrl.Arrange(new Rect(0, 0, width, height));
                ctrl.UpdateLayout();

                var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(ctrl);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));
                using var ms = new MemoryStream();
                encoder.Save(ms);
                return ms.ToArray();
            }
            catch
            {
                return null;
            }
        }
    }
}
