using CommunityToolkit.Mvvm.ComponentModel;

namespace ExtEnvAnalysis.Core;

public partial class SegmentState : ObservableObject, ISection
{
    [ObservableProperty] private string? segmentName;
    public bool IsValid => !string.IsNullOrWhiteSpace(SegmentName);

    public void Reset() => SegmentName = null;
}
