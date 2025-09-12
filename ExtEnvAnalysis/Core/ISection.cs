namespace ExtEnvAnalysis.Core;

public interface ISection
{
    bool IsValid { get; }
    void Reset();
}
