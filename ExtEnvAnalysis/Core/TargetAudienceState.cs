using CommunityToolkit.Mvvm.ComponentModel;

namespace ExtEnvAnalysis.Core;

public partial class TargetAudienceState : ObservableObject, ISection
{
    public bool IsValid { get; set; } = false;
    public void Reset() => IsValid = false;
}
