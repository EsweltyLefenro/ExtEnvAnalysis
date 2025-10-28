using ExtEnvAnalysis;
using ExtEnvAnalysis.Controls;
using ExtEnvAnalysis.Core;
using ExtEnvAnalysis.Models;
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
                                c.Item().Text(t => t.Span("Профиль").Bold().FontSize(14));
                                c.Item().Text($"ФИО: {app?.Profile?.FullName ?? ""}");
                                c.Item().Text($"Группа: {app?.Profile?.Group ?? ""}");
                                c.Item().Text($"Уровень: {LocalizeDifficulty(app?.Profile?.Difficulty ?? Difficulty.Bachelor)}");
                            }));

                        // 2) Сегмент
                        col.Item().PaddingBottom(10).Element(e =>
                            e.Column(c =>
                            {
                                c.Item().Text(t => t.Span("Сегмент").Bold().FontSize(14));
                                var seg = app?.Segment?.SegmentName ?? "";
                                c.Item().Text(string.IsNullOrWhiteSpace(seg) ? "—" : seg);
                            }));

                        // 3) PESTEL
                        col.Item().PaddingBottom(10).Element(e =>
                            e.Column(c =>
                            {
                                c.Item().Text(t => t.Span("PESTEL").Bold().FontSize(14));
                                PrintPestelList(c, "P — Политические", app?.Pestel, PestelType.P);
                                PrintPestelList(c, "E — Экономические", app?.Pestel, PestelType.E);
                                PrintPestelList(c, "S — Социальные", app?.Pestel, PestelType.S);
                                PrintPestelList(c, "T — Технологические", app?.Pestel, PestelType.T);
                                PrintPestelList(c, "E — Экологические", app?.Pestel, PestelType.Env);
                                PrintPestelList(c, "L — Правовые", app?.Pestel, PestelType.L);
                            }));

                        // --- новая страница перед "Факторы и веса"
                        col.Item().PageBreak();

                        // 4) Факторы и веса
                        col.Item().PaddingBottom(10).Element(e =>
                            e.Column(c =>
                            {
                                c.Item().Text(t => t.Span("Факторы и веса").Bold().FontSize(14));
                                var rows = app?.Factors?.Rows;
                                if (rows == null || rows.Count == 0)
                                {
                                    c.Item().Text("—");
                                }
                                else
                                {
                                    var active = rows.Where(r =>
                                        !string.IsNullOrWhiteSpace(r.Name) && r.WeightValue > 0).ToList();

                                    if (active.Count == 0) { c.Item().Text("—"); }
                                    else
                                    {
                                        c.Item().Table(t =>
                                        {
                                            t.ColumnsDefinition(cd =>
                                            {
                                                cd.RelativeColumn(3);
                                                cd.RelativeColumn(1);
                                            });
                                            t.Header(h =>
                                            {
                                                h.Cell().PaddingBottom(4).Text("Фактор").Bold();
                                                h.Cell().PaddingBottom(4).AlignRight().Text("Вес").Bold();
                                            });
                                            foreach (var r in active)
                                            {
                                                var wt = string.IsNullOrWhiteSpace(r.WeightText)
                                                    ? r.WeightValue.ToString("0.00")
                                                    : r.WeightText;
                                                t.Cell().PaddingVertical(2).Text(r.Name);
                                                t.Cell().PaddingVertical(2).AlignRight().Text(wt);
                                            }
                                        });
                                    }
                                }
                            }));

                        // 5) Оценка компаний
                        col.Item().PaddingBottom(10).Element(e =>
                            e.Column(c =>
                            {
                                c.Item().Text(t => t.Span("Оценка компаний").Bold().FontSize(14));
                                var rrows = app?.Ratings?.Rows;
                                if (rrows == null || rrows.Count == 0) { c.Item().Text("—"); return; }

                                var active = rrows.Where(row => row?.Factor != null
                                                        && !string.IsNullOrWhiteSpace(row.Factor.Name)
                                                        && row.Factor.WeightValue > 0).ToList();
                                if (active.Count == 0) { c.Item().Text("—"); return; }

                                c.Item().Table(t =>
                                {
                                    t.ColumnsDefinition(cd =>
                                    {
                                        cd.RelativeColumn(4);
                                        cd.RelativeColumn(1);
                                        cd.RelativeColumn(1);
                                        cd.RelativeColumn(1);
                                        cd.RelativeColumn(1);
                                    });

                                    var (nMy, nA, nB, nC) = CompanyNames(app?.Ratings);
                                    t.Header(h =>
                                    {
                                        h.Cell().PaddingBottom(4).Text("Фактор").Bold();
                                        h.Cell().PaddingBottom(4).AlignCenter().Text(nMy).Bold();
                                        h.Cell().PaddingBottom(4).AlignCenter().Text(nA).Bold();
                                        h.Cell().PaddingBottom(4).AlignCenter().Text(nB).Bold();
                                        h.Cell().PaddingBottom(4).AlignCenter().Text(nC).Bold();
                                    });

                                    foreach (var row in active)
                                    {
                                        string F(int v) => v == 0 ? "" : v.ToString("0");
                                        t.Cell().PaddingVertical(2).Text(row.Factor.Name);
                                        t.Cell().AlignCenter().Text(F(row.MyValue));
                                        t.Cell().AlignCenter().Text(F(row.AValue));
                                        t.Cell().AlignCenter().Text(F(row.BValue));
                                        t.Cell().AlignCenter().Text(F(row.CValue));
                                    }
                                });
                                // === Мини-сводка по компаниям: Доля рынка + Взвеш. рейтинг ===
                                {
                                    double sumW = active.Sum(x => x.Factor.WeightValue);
                                    double Score(Func<Core.RatingRow, int> sel) => active.Sum(x => x.Factor.WeightValue * sel(x));
                                    double N(double s) => sumW > 0 ? s / sumW : 0.0;

                                    double my = N(Score(x => x.MyValue));
                                    double a = N(Score(x => x.AValue));
                                    double b = N(Score(x => x.BValue));
                                    double c0 = N(Score(x => x.CValue));

                                    var shares = app?.Ratings != null ? GetShares01FromRatings(app.Ratings) : new double[] { 0, 0, 0, 0 };
                                    double shMy = shares.ElementAtOrDefault(0);
                                    double shA = shares.ElementAtOrDefault(1);
                                    double shB = shares.ElementAtOrDefault(2);
                                    double shC = shares.ElementAtOrDefault(3);

                                    c.Item().PaddingTop(8).Text(x => x.Span("Сводка по компаниям").Bold());

                                    c.Item().Table(t =>
                                    {
                                        t.ColumnsDefinition(cd =>
                                        {
                                            cd.RelativeColumn(2.0f);  // Компания
                                            cd.RelativeColumn(1.2f);  // Доля
                                            cd.RelativeColumn(1.6f);  // Рейтинг
                                        });
                                        t.Header(h =>
                                        {
                                            h.Cell().PaddingBottom(4).Text("Компания").Bold();
                                            h.Cell().PaddingBottom(4).AlignRight().Text("Доля рынка").Bold();
                                            h.Cell().PaddingBottom(4).AlignRight().Text("Взвеш. рейтинг (0–10)").Bold();
                                        });

                                        void Row(string name, double share01, double score)
                                        {
                                            t.Cell().PaddingVertical(2).Text(name);
                                            t.Cell().PaddingVertical(2).AlignRight().Text((share01 * 100.0).ToString("0.0") + " %");
                                            t.Cell().PaddingVertical(2).AlignRight().Text(score.ToString("0.00"));
                                        }

                                        var (nMy2, nA2, nB2, nC2) = CompanyNames(app?.Ratings);

                                        Row(nMy2, shMy, my);
                                        Row(nA2, shA, a);
                                        Row(nB2, shB, b);
                                        Row(nC2, shC, c0);
                                    });
                                }

                            }));

                        // 6) Квадратные карты + пояснения/направления
                        var maps = app?.Comparisons?.Maps;
                        if (maps != null && maps.Any())
                        {
                            int i = 1;
                            foreach (var map in maps)
                            {
                                col.Item().PaddingTop(10).Text($"Стратегическая карта {i}").Bold().FontSize(14);
                                var png = RenderMapToPng(map, 800, 800);
                                if (png != null) col.Item().Height(300).Image(png);
                                col.Item().PaddingTop(6).Text("Пояснение").Bold();
                                col.Item().Text(map.Explanation ?? "");
                                col.Item().PaddingTop(6).Text("");
                                col.Item().Text("Направление развития").Bold();
                                col.Item().Text(string.IsNullOrWhiteSpace(map.Direction) ? i.ToString() : map.Direction);
                                i++;
                                col.Item().PageBreak();
                            }
                        }

                        // 7) Итоги: Доля рынка + Взвешенный рейтинг
                        col.Item().PaddingTop(10).Element(e =>
                            e.Column(c =>
                            {
                                c.Item().Text(t => t.Span("Итоги по компаниям").Bold().FontSize(14));

                                // === Текстовые Итоги из вкладки 6 ===
                                var conclusion = app?.Report?.Conclusion;
                                if (!string.IsNullOrWhiteSpace(conclusion))
                                {
                                    c.Item().PaddingTop(8).Text(t => t.Span("Итоги").Bold());
                                    c.Item().Text(conclusion);
                                    c.Item().PaddingBottom(12).Text("");
                                }

                                var rrows = app?.Ratings?.Rows;
                                if (rrows == null || rrows.Count == 0) { c.Item().Text("—"); return; }

                                var active = rrows.Where(row => row?.Factor != null
                                                        && !string.IsNullOrWhiteSpace(row.Factor.Name)
                                                        && row.Factor.WeightValue > 0).ToList();
                                if (active.Count == 0) { c.Item().Text("—"); return; }

                                double sumW = active.Sum(x => x.Factor.WeightValue);
                                double Score(Func<Core.RatingRow, int> sel) => active.Sum(x => x.Factor.WeightValue * sel(x));
                                double N(double s) => sumW > 0 ? s / sumW : 0.0;

                                double my = N(Score(x => x.MyValue));
                                double a = N(Score(x => x.AValue));
                                double b = N(Score(x => x.BValue));
                                double c0 = N(Score(x => x.CValue));

                                // доли рынка (0..1) из RatingsState
                                var shares = app?.Ratings != null ? GetShares01FromRatings(app.Ratings) : new double[] { 0, 0, 0, 0 };
                                double shMy = shares.ElementAtOrDefault(0);
                                double shA = shares.ElementAtOrDefault(1);
                                double shB = shares.ElementAtOrDefault(2);
                                double shC = shares.ElementAtOrDefault(3);

                                c.Item().Table(t =>
                                {
                                    t.ColumnsDefinition(cd =>
                                    {
                                        cd.RelativeColumn(2.0f); // Компания
                                        cd.RelativeColumn(1.2f); // Доля рынка
                                        cd.RelativeColumn(1.4f); // Взвешенный рейтинг
                                    });
                                    t.Header(h =>
                                    {
                                        h.Cell().PaddingBottom(4).Text("Компания").Bold();
                                        h.Cell().PaddingBottom(4).AlignRight().Text("Доля рынка").Bold();
                                        h.Cell().PaddingBottom(4).AlignRight().Text("Взвешенный рейтинг (0–10)").Bold();
                                    });

                                    void Row(string name, double share01, double score)
                                    {
                                        t.Cell().PaddingVertical(2).Text(name);
                                        t.Cell().PaddingVertical(2).AlignRight().Text((share01 * 100.0).ToString("0.0") + " %");
                                        t.Cell().PaddingVertical(2).AlignRight().Text(score.ToString("0.00"));
                                    }

                                    var (nMy3, nA3, nB3, nC3) = CompanyNames(app?.Ratings);

                                    Row(nMy3, shMy, my);
                                    Row(nA3, shA, a);
                                    Row(nB3, shB, b);
                                    Row(nC3, shC, c0);
                                });

                                // Имена компаний из состояния (с дефолтами)
                                var nMy3 = app?.Ratings?.CompanyMyName ?? "Мы";
                                var nA3 = app?.Ratings?.CompanyAName ?? "A";
                                var nB3 = app?.Ratings?.CompanyBName ?? "B";
                                var nC3 = app?.Ratings?.CompanyCName ?? "C";

                                var best = new[]
                                {
                                    (nMy3, my),
                                    (nA3,  a),
                                    (nB3,  b),
                                    (nC3,  c0)
                                }.OrderByDescending(x => x.Item2).ToList();

                                var top = best.First();
                                var ties = best.Where(x => Math.Abs(x.Item2 - top.Item2) < 1e-9).Select(x => x.Item1).ToList();

                                if (ties.Count > 1)
                                    c.Item().PaddingTop(6).Text($"Лидеры по рейтингу: {string.Join(", ", ties)} (по {top.Item2:0.00}).");
                                else
                                    c.Item().PaddingTop(6).Text($"Лидер по рейтингу: {top.Item1} ({top.Item2:0.00}).");
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
            }).GeneratePdf(file);

            return file;
        }

        private static string LocalizeDifficulty(Difficulty d) => d switch
        {
            Difficulty.Bachelor => "Бакалавр",
            Difficulty.Master => "Магистр",
            Difficulty.Developer => "Разработчик",
            _ => d.ToString()
        };

        private static void PrintPestelList(ColumnDescriptor col, string title, PestelState? state, PestelType type)
        {
            col.Item().Text(x => x.Span(title).Bold());
            var lines = GetPestelLines(state, type);
            if (lines.Length == 0) { col.Item().Text("—"); return; }
            foreach (var s in lines) col.Item().Text("• " + s);
        }

        private static string[] GetPestelLines(PestelState? state, PestelType type)
        {
            if (state == null) return Array.Empty<string>();
            var cat = state.Categories.FirstOrDefault(c => c.Type == type);
            if (cat == null) return Array.Empty<string>();
            return new[] { cat.Field1, cat.Field2, cat.Field3, cat.Field4, cat.Field5 }
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim()).ToArray();
        }

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
            catch { return null; }
        }

        // --- ХЕЛПЕР: вытянуть доли рынка из RatingsState при любых названиях полей/типах
        private static double[] GetShares01FromRatings(RatingsState ratings)
        {
            // 1) сначала пробуем числовые свойства MarketShareMy/A/B/C (в процентах 0..100)
            double? getDouble(string name)
            {
                var p = ratings.GetType().GetProperty(name);
                if (p == null) return null;
                var v = p.GetValue(ratings);
                if (v is double d) return d;
                if (v is float f) return (double)f;
                if (v is int i) return (double)i;
                if (v is string s && double.TryParse(s.Trim().TrimEnd('%'), out var d2)) return d2;
                return null;
            }

            double clamp01(double perc) => Math.Max(0, Math.Min(100, perc)) / 100.0;

            var mMy = getDouble("MarketShareMy");
            var mA = getDouble("MarketShareA");
            var mB = getDouble("MarketShareB");
            var mC = getDouble("MarketShareC");

            if (mMy.HasValue && mA.HasValue && mB.HasValue && mC.HasValue)
                return new[] { clamp01(mMy.Value), clamp01(mA.Value), clamp01(mB.Value), clamp01(mC.Value) };

            // 2) иначе пробуем текстовые MarketMyText/AText/BText/CText (целые 0..100 без знака %)
            string? getString(string name)
            {
                var p = ratings.GetType().GetProperty(name);
                return p?.GetValue(ratings) as string;
            }

            double fromText(string? s)
            {
                if (string.IsNullOrWhiteSpace(s)) return 0;
                var t = s.Trim().TrimEnd('%');
                if (!int.TryParse(t, out var v)) return 0;
                if (v < 0) v = 0; if (v > 100) v = 100;
                return v / 100.0;
            }

            var tMy = getString("MarketMyText");
            var tA = getString("MarketAText");
            var tB = getString("MarketBText");
            var tC = getString("MarketCText");

            return new[]
            {
        fromText(tMy),
        fromText(tA),
        fromText(tB),
        fromText(tC)
    };
        }

        // имена компаний из состояния; даём дефолты
        private static (string nMy, string nA, string nB, string nC) CompanyNames(ExtEnvAnalysis.Core.RatingsState? r)
        {
            return (r?.CompanyMyName ?? "Мы",
                    r?.CompanyAName ?? "A",
                    r?.CompanyBName ?? "B",
                    r?.CompanyCName ?? "C");
        }

    }
}
