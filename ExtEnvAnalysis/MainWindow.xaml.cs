using ExtEnvAnalysis.Core;
using ExtEnvAnalysis.Models;
using ExtEnvAnalysis.Services;
using ExtEnvAnalysis.ViewModels;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.ComponentModel;


namespace ExtEnvAnalysis;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Raise([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    public MainViewModel VM { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = VM;

        // когда модель говорит «правила изменились» — обновляем UI
        VM.App.RulesChanged += () => Dispatcher.Invoke(VM.NotifyRulesChanged);

        // как только меняется сумма или валидность факторов — обновляем доступ к вкладкам
        VM.App.Factors.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ExtEnvAnalysis.Core.FactorsState.Sum) ||
                e.PropertyName == nameof(ExtEnvAnalysis.Core.FactorsState.IsValid))
            {
                Dispatcher.Invoke(VM.NotifyRulesChanged);
            }
        };
        VM.App.Ratings.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ExtEnvAnalysis.Core.RatingsState.IsValid))
                Dispatcher.Invoke(VM.NotifyRulesChanged);
        };
        VM.App.Ratings.AttachToFactors(VM.App.Factors);
    }

    // общий обработчик для удаления/очистки для всех вариаций кнопки
    private void FactorDeleteOrClear(FactorRow row)
    {
        if (VM.App.Profile.Difficulty == Difficulty.Bachelor)
        {
            // бакалавр: фактор оставляем, вес очищаем
            row.WeightText = "";                 // пусто => вес 0
            VM.App.FactorsChanged();             // пересчёт сумм, рейтингов и карт
            VM.NotifyRulesChanged();
        }
        else
        {
            // магистр/разработчик: удаляем строку факторов
            VM.App.Factors.RemoveRow(row);
            VM.App.FactorsChanged();
            VM.NotifyRulesChanged();
        }
    }

    private void Factor_Delete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is FactorRow row)
            FactorDeleteOrClear(row);
    }

    private void FactorRemove_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is FactorRow row)
            FactorDeleteOrClear(row);
    }

    private void Segment_TextChanged(object sender, TextChangedEventArgs e)
    {
        VM.App.ChangeSegment(VM.App.Segment.SegmentName);
        VM.NotifyRulesChanged();
    }

    private void Ta_Changed(object sender, RoutedEventArgs e) => VM.NotifyRulesChanged();

    private void Comparisons_Changed(object sender, RoutedEventArgs e) => VM.NotifyRulesChanged();

    private void PestelField_TextChanged(object sender, TextChangedEventArgs e)
    {
        VM.App.Pestel.Recalculate();
        VM.NotifyRulesChanged();
    }

    private void DifficultyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (!Enum.TryParse<Difficulty>(btn.Tag?.ToString(), out var newLevel)) return;

        var current = VM.App.Profile.Difficulty;
        if (current == newLevel) return;

        // Предупреждение: откат всего, кроме ФИО и группы
        var res = MessageBox.Show(
            "Сменить уровень? Все введённые данные (кроме ФИО и группы) будут удалены.",
            "Подтвердите смену уровня",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (res != MessageBoxResult.Yes) return;

        // (Здесь позже легко вставить проверку пароля)
        // if (!CheckPasswordFor(newLevel)) return;

        // Сохраняем ФИО/Группу, сбрасываем всё остальное и применяем новый уровень
        var fullName = VM.App.Profile.FullName;
        var group = VM.App.Profile.Group;

        VM.App.ResetAllExceptProfile();              // <-- см. п.3
        VM.App.Profile.FullName = fullName;
        VM.App.Profile.Group = group;
        VM.App.Profile.Difficulty = newLevel;

        VM.SelectedTabIndex = 0;
        VM.NotifyRulesChanged();
    }

    // Вес меняется: фильтруем ввод и пересчитываем
    private static readonly Regex weightAllowed = new(@"[^0-9\.,]", RegexOptions.Compiled);

    private void SegmentChoice_Click(object sender, RoutedEventArgs e)
    {
        var btn = (ButtonBase)sender; // Button или ToggleButton
        string name = btn.Tag?.ToString() ?? btn.Content?.ToString();
        if (string.IsNullOrWhiteSpace(name)) return;

        VM.App.ChangeSegment(name);
        VM.NotifyRulesChanged();
    }


    static readonly CultureInfo Ru = new("ru-RU");
    static readonly Regex Allowed = new(@"^\d*(,\d{0,2})?$"); // цифры + запятая, до 2 знаков после

    // === STEP 4: Ввод весов (валидация ввода и нормализация) ===
    private void FactorWeight_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is not TextBox tb) return;

        string incoming = e.Text ?? string.Empty;
        if (incoming.Length == 0) { e.Handled = true; return; }

        char ch = incoming[0];

        // локальные помощники
        static void InsertLiteral(TextBox t, string lit)
        {
            int start = t.SelectionStart;
            int len = t.SelectionLength;

            string txt = t.Text ?? string.Empty;
            if (len > 0) txt = txt.Remove(start, len);
            txt = txt.Insert(start, lit);

            t.Text = txt;
            t.CaretIndex = start + lit.Length;
        }

        bool alreadyHasCommaOutsideSelection =
            (tb.Text ?? string.Empty).Contains(",") &&
            !(tb.SelectedText ?? string.Empty).Contains(",");

        // Знак разделителя: '.' -> ','; ',' оставляем как есть
        if (ch == '.' || ch == ',')
        {
            // Разрешаем только одну запятую в результате
            if (alreadyHasCommaOutsideSelection)
            {
                e.Handled = true; // вторую запятую/точку блокируем
                return;
            }

            e.Handled = true;     // сами вставим нужный символ
            InsertLiteral(tb, ",");
            return;
        }

        // Разрешены только цифры
        if (char.IsDigit(ch))
        {
            e.Handled = false;
            return;
        }

        e.Handled = true; // всё остальное запрещаем
    }

    private void FactorWeight_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is FactorRow row)
        {
            row.FormatWeightText();
            VM.App.Factors.Recalculate(VM.App.Profile.Difficulty);

            VM.App.FactorsChanged();      // <-- пересоберёт рейтинги и карты
            VM.NotifyRulesChanged();
        }
    }

    // Добавить новую строку факторов (магистр/разработчик)
    private void FactorAdd_Click(object sender, RoutedEventArgs e)
    {
        VM.App.Factors.AddRow();
        VM.App.Factors.Recalculate(VM.App.Profile.Difficulty);

        VM.App.FactorsChanged();      // NEW
        VM.NotifyRulesChanged();
    }

    // === STEP 5: Оценки 1..10 ===
    private void Score_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = e.Text is null || e.Text.Length == 0 || !char.IsDigit(e.Text[0]);
    }
    private void Score_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox tb) return;
        var s = tb.Text?.Trim();
        if (string.IsNullOrEmpty(s)) return;
        if (!int.TryParse(s, out var v)) return;
        if (v < 1) v = 1;
        if (v > 10) v = 10;
        tb.Text = v.ToString();
        VM.App.RatingsChanged();
        VM.NotifyRulesChanged();
    }

    // 0..100 (проценты)
    private void Percent_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = e.Text is null || e.Text.Length == 0 || !char.IsDigit(e.Text[0]);
    }
    private void Percent_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox tb) return;
        var s = tb.Text?.Trim();
        if (string.IsNullOrEmpty(s)) return;
        if (!int.TryParse(s, out var v)) return;
        if (v < 0) v = 0;
        if (v > 100) v = 100;
        tb.Text = v.ToString();
        VM.App.RatingsChanged();
        VM.NotifyRulesChanged();

    }

    private void Explanation_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is MapModel m && string.IsNullOrWhiteSpace(tb.Text))
            tb.Text = $"X: {m.TitleX}\nY: {m.TitleY}";
    }

    private void Direction_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.NotifyRulesChanged(); // этот метод уже должен дергать OnPropertyChanged для CanTab*
    }

    // 7 вкл
    public string? ReportFilePath { get; set; }
    public bool ReportHasFile => !string.IsNullOrWhiteSpace(ReportFilePath) && File.Exists(ReportFilePath);

    private void UpdateReportBindings()
    {
        Raise(nameof(ReportFilePath));
        Raise(nameof(ReportHasFile));
    }

    private void Report_Build_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            try
            {
                // Предполагаю, что итоговый текст хранится в vm.App.Report.Conclusion
                // (мы его уже биндили на вкладке 6 в tbFinalSummary).
                // Если имя другое — поправь в ReportBuilder.
                string path = ReportBuilder.BuildPdf(vm.App);
                ReportFilePath = path;
                UpdateReportBindings();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Ошибка формирования отчёта:\n{ex.Message}", "Отчёт", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void Report_Open_Click(object sender, RoutedEventArgs e)
    {
        if (ReportHasFile)
        {
            try { Process.Start(new ProcessStartInfo(ReportFilePath!) { UseShellExecute = true }); }
            catch (Exception ex)
            { MessageBox.Show(this, $"Не удалось открыть файл:\n{ex.Message}"); }
        }
    }

    private void Report_SaveAs_Click(object sender, RoutedEventArgs e)
    {
        if (!ReportHasFile) return;
        var dlg = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = Path.GetFileName(ReportFilePath)
        };
        if (dlg.ShowDialog(this) == true)
        {
            try { File.Copy(ReportFilePath!, dlg.FileName, overwrite: true); }
            catch (Exception ex)
            { MessageBox.Show(this, $"Не удалось сохранить:\n{ex.Message}"); }
        }
    }
}
