using CommunityToolkit.Mvvm.ComponentModel;
using ExtEnvAnalysis.Core;
using ExtEnvAnalysis.Services;
using ExtEnvAnalysis.ViewModels;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

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

        var vm = DataContext as ExtEnvAnalysis.ViewModels.MainViewModel;
        if (vm == null) return;

        VM.App.RulesChanged += () => Dispatcher.Invoke(VM.NotifyRulesChanged);

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

    private void FactorDeleteOrClear(FactorRow row)
    {
        if (VM.App.Profile.Difficulty is Difficulty.Bachelor or Difficulty.Developer)
        {
            row.WeightText = "";
            VM.App.FactorsChanged();
            VM.NotifyRulesChanged();
        }
        else
        {
            VM.App.Factors.RemoveRow(row);
            VM.App.FactorsChanged();
            VM.NotifyRulesChanged();
        }
    }

    private void FactorRemove_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is FactorRow row)
            FactorDeleteOrClear(row);
    }

    private void Segment_TextChanged(object sender, TextChangedEventArgs e)
    {
        VM.App.ChangeSegment(VM.App.Segment.SegmentName);
        ClearReportFile();
        VM.NotifyRulesChanged();
    }

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

        var res = MessageBox.Show(
            "Сменить уровень? Все введённые данные (кроме полей «Фамилия и имя» и «Группа») будут удалены.",
            "Подтвердите смену уровня",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (res != MessageBoxResult.Yes) return;

        var fullName = VM.App.Profile.FullName;
        var group = VM.App.Profile.Group;

        VM.App.ResetAllExceptProfile();
        VM.App.Profile.FullName = fullName;
        VM.App.Profile.Group = group;
        VM.App.Profile.Difficulty = newLevel;
        ClearReportFile();

        if (newLevel == Difficulty.Developer)
            VM.App.ApplyDeveloperPreset();

        VM.SelectedTabIndex = 0;
        VM.NotifyRulesChanged();
    }

    private void SegmentChoice_Click(object sender, RoutedEventArgs e)
    {
        var btn = (ButtonBase)sender;
        string name = btn.Tag?.ToString() ?? btn.Content?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name)) return;
        if (string.Equals(VM.App.Segment.SegmentName, name, StringComparison.Ordinal)) return;

        VM.App.ChangeSegment(name);
        ClearReportFile();
        VM.NotifyRulesChanged();
    }

    private void FactorWeight_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is not TextBox tb) return;

        string incoming = e.Text ?? string.Empty;
        if (incoming.Length == 0) { e.Handled = true; return; }

        char ch = incoming[0];

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

        if (ch == '.' || ch == ',')
        {
            if (alreadyHasCommaOutsideSelection)
            {
                e.Handled = true;
                return;
            }

            e.Handled = true;
            InsertLiteral(tb, ",");
            return;
        }

        if (char.IsDigit(ch))
        {
            e.Handled = false;
            return;
        }

        e.Handled = true;
    }

    private void FactorWeight_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is FactorRow row)
        {
            row.FormatWeightText();
            VM.App.Factors.Recalculate(VM.App.Profile.Difficulty);

            VM.App.FactorsChanged();
            VM.NotifyRulesChanged();
        }
    }

    private void FactorAdd_Click(object sender, RoutedEventArgs e)
    {
        VM.App.Factors.AddRow();
        VM.App.Factors.Recalculate(VM.App.Profile.Difficulty);

        VM.App.FactorsChanged();
        VM.NotifyRulesChanged();
    }

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

    private void Direction_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.NotifyRulesChanged();
    }

    public string? ReportFilePath { get; set; }
    public bool ReportHasFile => !string.IsNullOrWhiteSpace(ReportFilePath) && File.Exists(ReportFilePath);

    private void UpdateReportBindings()
    {
        Raise(nameof(ReportFilePath));
        Raise(nameof(ReportHasFile));
    }

    private void ClearReportFile()
    {
        ReportFilePath = null;
        UpdateReportBindings();
    }

    private void Report_Build_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        vm.App.Factors.Recalculate(vm.App.Profile.Difficulty);
        vm.App.Ratings.Recalculate();

        var errors = vm.App.GetBlockingErrorsForReport();
        if (errors.Count > 0)
        {
            MessageBox.Show("Нужно исправить:\n\n• " + string.Join("\n• ", errors),
                            "Проверки перед отчётом",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            string path = ReportBuilder.BuildPdf(vm.App);
            ReportFilePath = path;
            UpdateReportBindings();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка генерации отчёта:\n" + ex.Message,
                            "ReportBuilder", MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (string.IsNullOrWhiteSpace(dlg.FileName)) return;

            try { File.Copy(ReportFilePath!, dlg.FileName, overwrite: true); }
            catch (Exception ex)
            { MessageBox.Show(this, $"Не удалось сохранить:\n{ex.Message}"); }
        }
    }

}
