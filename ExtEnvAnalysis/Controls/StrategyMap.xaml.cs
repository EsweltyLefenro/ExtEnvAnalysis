using System;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ExtEnvAnalysis.Models;

namespace ExtEnvAnalysis.Controls
{
    public partial class StrategyMap : UserControl
    {
        public StrategyMap()
        {
            InitializeComponent();
            SizeChanged += (_, __) => Redraw();
        }

        // === DependencyProperty для модели карты ===
        public static readonly DependencyProperty MapProperty =
            DependencyProperty.Register(
                nameof(Map),
                typeof(MapModel),
                typeof(StrategyMap),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnMapChanged));

        public MapModel? Map
        {
            get => (MapModel?)GetValue(MapProperty);
            set => SetValue(MapProperty, value);
        }

        private static void OnMapChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StrategyMap self)
                self.Redraw();
        }

        // --- утилита: попытаться достать название оси из Map по распространённым именам свойств
        private string GetAxisTitle(bool isX)
        {
            if (Map == null) return isX ? "X" : "Y";
            string[] candidates = isX
                ? new[] { "AxisX", "XLabel", "XName", "XTitle", "XAxis", "FactorX", "TitleX" }
                : new[] { "AxisY", "YLabel", "YName", "YTitle", "YAxis", "FactorY", "TitleY" };

            foreach (var name in candidates)
            {
                var prop = Map.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
                if (prop != null && prop.PropertyType == typeof(string))
                {
                    var val = prop.GetValue(Map) as string;
                    if (!string.IsNullOrWhiteSpace(val)) return val!;
                }
            }
            return isX ? "X" : "Y";
        }

        // === Основной метод отрисовки ===
        private void Redraw()
        {
            if (Plot == null || Map == null) return;

            Plot.Children.Clear();

            // Размеры Canvas — как есть (квадратность задаётся в XAML)
            double W = Math.Max(ActualWidth, 200);
            double H = Math.Max(ActualHeight, 200);
            Plot.Width = W;
            Plot.Height = H;

            // ФИКСИРОВАННЫЕ ПОЛЯ (НЕ МЕНЯЕМ)
            const double left = 90;
            const double right = 24;
            const double top = 28;
            const double bottom = 90;

            double gw = W - left - right; // ширина сетки
            double gh = H - top - bottom; // высота сетки
            if (gw <= 0 || gh <= 0) return;

            const int N = 10; // 10×10
            double cellW = gw / N;
            double cellH = gh / N;

            // ===== Сетка 10×10 =====
            var gridBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220));
            for (int i = 0; i <= N; i++)
            {
                double x = left + i * cellW;
                Plot.Children.Add(new System.Windows.Shapes.Line
                {
                    X1 = x,
                    Y1 = top,
                    X2 = x,
                    Y2 = top + gh,
                    Stroke = gridBrush,
                    StrokeThickness = 1
                });

                double y = top + gh - i * cellH;
                Plot.Children.Add(new System.Windows.Shapes.Line
                {
                    X1 = left,
                    Y1 = y,
                    X2 = left + gw,
                    Y2 = y,
                    Stroke = gridBrush,
                    StrokeThickness = 1
                });
            }

            // Рамка вокруг поля
            Plot.Children.Add(new System.Windows.Shapes.Rectangle
            {
                Width = gw,
                Height = gh,
                Stroke = Brushes.Black,
                StrokeThickness = 1.5
            });
            Canvas.SetLeft(Plot.Children[^1], left);
            Canvas.SetTop(Plot.Children[^1], top);

            // ===== Числа по осям (1..10), центрированные по клеткам =====
            void DrawCenteredText(string text, double cx, double cy, double fontSize = 18, bool bold = false)
            {
                var tb = new TextBlock
                {
                    Text = text,
                    FontSize = fontSize,
                    FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal
                };
                tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(tb, cx - tb.DesiredSize.Width / 2.0);
                Canvas.SetTop(tb, cy - tb.DesiredSize.Height / 2.0);
                Plot.Children.Add(tb);
            }

            // Ось X: числа внизу
            for (int i = 1; i <= N; i++)
            {
                double cx = left + (i - 0.5) * cellW;
                double cy = top + gh + 34;
                DrawCenteredText(i.ToString(), cx, cy, 18, bold: false);

                double tickX = left + i * cellW;
                Plot.Children.Add(new System.Windows.Shapes.Line
                {
                    X1 = tickX,
                    Y1 = top + gh,
                    X2 = tickX,
                    Y2 = top + gh + 6,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                });
            }

            // Ось Y: числа слева
            for (int j = 1; j <= N; j++)
            {
                double cy = top + gh - (j - 0.5) * cellH;
                double cx = left - 34;
                DrawCenteredText(j.ToString(), cx, cy, 18, bold: false);

                double tickY = top + gh - j * cellH;
                Plot.Children.Add(new System.Windows.Shapes.Line
                {
                    X1 = left - 6,
                    Y1 = tickY,
                    X2 = left,
                    Y2 = tickY,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                });
            }

            // ==== Подписи осей ====
            string xTitle = GetAxisTitle(isX: true);
            string yTitle = GetAxisTitle(isX: false);

            // X — по центру снизу
            {
                var tb = new TextBlock { Text = xTitle, FontSize = 16, FontWeight = FontWeights.SemiBold };
                tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                double cx = left + gw / 2.0;
                double cy = top + gh + 58;
                Canvas.SetLeft(tb, cx - tb.DesiredSize.Width / 2.0);
                Canvas.SetTop(tb, cy - tb.DesiredSize.Height / 2.0);
                Plot.Children.Add(tb);
            }

            // Y — вертикально, по центру слева
            {
                var tb = new TextBlock { Text = yTitle, FontSize = 16, FontWeight = FontWeights.SemiBold, LayoutTransform = new RotateTransform(-90) };
                tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                double cx = left - 58;
                double cy = top + gh / 2.0;
                Canvas.SetLeft(tb, cx - tb.DesiredSize.Width / 2.0);
                Canvas.SetTop(tb, cy - tb.DesiredSize.Height / 2.0);
                Plot.Children.Add(tb);
            }

            // ===== Перевод координат 1..10 в пиксели =====
            double ToPxX(double x) => left + (Math.Clamp(x, 1.0, 10.0) - 0.5) * cellW;
            double ToPxY(double y) => top + gh - (Math.Clamp(y, 1.0, 10.0) - 0.5) * cellH;

            // ==== Масштаб кружков от самой большой доли рынка ====
            double maxMarket = Math.Max(Math.Max(Map.Me.Market, Map.A.Market), Math.Max(Map.B.Market, Map.C.Market));
            if (maxMarket <= 0) maxMarket = 1; // защита от деления на ноль
            double cellSize = Math.Min(cellW, cellH);
            double Rmax = 0.48 * cellSize; // крупнейший ~ почти вся клетка
            double Rmin = 0.14 * cellSize; // у маленьких, чтобы не исчезли

            // ===== Рисование точки компании =====
            void DrawPoint(CompanyPoint p, string label, Brush brush)
            {
                double px = ToPxX(p.X);
                double py = ToPxY(p.Y);

                double rRatio = p.Market / maxMarket;   // 0..1
                double R = Math.Max(Rmin, Rmax * rRatio);

                var ellipse = new System.Windows.Shapes.Ellipse
                {
                    Width = 2 * R,
                    Height = 2 * R,
                    Stroke = brush,
                    Fill = new SolidColorBrush(Color.FromArgb(32,
                        ((SolidColorBrush)brush).Color.R,
                        ((SolidColorBrush)brush).Color.G,
                        ((SolidColorBrush)brush).Color.B)),
                    StrokeThickness = 1.6
                };
                ellipse.ToolTip = $"{label}: X={p.X:0}, Y={p.Y:0}, Доля рынка={p.Market:0}%";

                Canvas.SetLeft(ellipse, px - R);
                Canvas.SetTop(ellipse, py - R);
                Plot.Children.Add(ellipse);

                var tblock = new TextBlock
                {
                    Text = label,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12,  // чуть меньше
                    Foreground = brush
                };
                tblock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(tblock, px + R + 4); // ближе к окружности
                Canvas.SetTop(tblock, py - tblock.DesiredSize.Height / 2.0);
                Plot.Children.Add(tblock);
            }

            // Точки компаний
            DrawPoint(Map.Me, "Мы", Brushes.RoyalBlue);
            DrawPoint(Map.A, "A", Brushes.Goldenrod);
            DrawPoint(Map.B, "B", Brushes.MediumSeaGreen);
            DrawPoint(Map.C, "C", Brushes.IndianRed);
        }
    }
}
