using CommunityToolkit.Mvvm.ComponentModel;

namespace ExtEnvAnalysis.Core;

public partial class TargetAudienceState : ObservableObject, ISection
{
    // Заглушка: формы ЦА (персона, боли/ожидания и т.д.) — заполним позже
    public bool IsValid { get; set; } = false;
    public void Reset() => IsValid = false;   // Обычно не сбрасываем. Используется для навигации.
}
