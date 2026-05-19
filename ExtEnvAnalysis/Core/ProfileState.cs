using CommunityToolkit.Mvvm.ComponentModel;

namespace ExtEnvAnalysis.Core;

public partial class ProfileState : ObservableObject, ISection
{
    [ObservableProperty] private string? fullName;
    [ObservableProperty] private string? group;
    [ObservableProperty] private Difficulty difficulty = Difficulty.Bachelor;

    public bool IsDeveloper => Difficulty == Difficulty.Developer;

    public bool IsValid => true;

    public void Reset()
    {
    }
}
