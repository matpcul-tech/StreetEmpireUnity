namespace StreetEmpire.Sim
{
    /// <summary>
    /// The exact LCG from state.js: rng = (rng * 1664525 + 1013904223) mod 2^32, value = rng / 2^32.
    /// JS does this in doubles, which stay exact below 2^53, so ulong math gives identical numbers.
    /// Keep it bit for bit: replays, tests and server authority all depend on it.
    /// </summary>
    public static class SimRng
    {
        const ulong Mod = 4294967296UL;
        public static ulong Seed(long seed) => (ulong)(((seed % (long)Mod) + (long)Mod) % (long)Mod);
        public static double Next(SimState s)
        {
            s.rng = (s.rng * 1664525UL + 1013904223UL) % Mod;
            return s.rng / (double)Mod;
        }
    }
}
