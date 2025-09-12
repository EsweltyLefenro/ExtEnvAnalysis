using CommunityToolkit.Mvvm.ComponentModel;

namespace ExtEnvAnalysis.Core;

public partial class ProfileState : ObservableObject, ISection
{
    [ObservableProperty] private string? fullName;
    [ObservableProperty] private string? group;
    [ObservableProperty] private Difficulty difficulty = Difficulty.Bachelor;

    public bool IsDeveloper => Difficulty == Difficulty.Developer;

    // Профиль всегда «валиден» (ФИО/группа валидируются отдельно в UI при надобности)
    public bool IsValid => true;

    public void Reset()
    {
        // Профиль очищать не надо по нашим правилам.
    }
}
