using CommunityToolkit.Mvvm.ComponentModel;

namespace ExtEnvAnalysis.Core;

public partial class ReportState : ObservableObject, ISection
{
    public bool IsValid { get; set; } = false;
    public void Reset()
    {
        IsValid = false;
        Conclusion = null;
    }

    [ObservableProperty] private string? conclusion;
}
