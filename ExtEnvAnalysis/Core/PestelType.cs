namespace ExtEnvAnalysis.Core
{
    public enum PestelType { P, E, S, T, Env, L }

    public static class PestelTypeExt
    {
        public static string Display(this PestelType t) => t switch
        {
            PestelType.P => "P — Political",
            PestelType.E => "E — Economic",
            PestelType.S => "S — Social",
            PestelType.T => "T — Technological",
            PestelType.Env => "E — Environmental",
            PestelType.L => "L — Legal",
            _ => ""
        };
    }
}
