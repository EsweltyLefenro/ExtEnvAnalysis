namespace ExtEnvAnalysis.Core
{
    public static class DifficultyExtensions
    {
        public static bool IsBachelorLike(this Difficulty d) =>
            d == Difficulty.Bachelor || d == Difficulty.Developer;
    }
}
