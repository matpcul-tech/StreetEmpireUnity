namespace StreetEmpire.Sim
{
    /// <summary>
    /// Source data is in three.js space (right handed, y up). Unity is left handed. glTFast imports
    /// GLB files by negating X, so positions do the same: unity = (-x, y, z), and yaw flips sign.
    /// Do every conversion through here so models and data stay lined up.
    /// </summary>
    public static class WorldSpace
    {
        public static (float x, float z) FromSource(float x, float z) => (-x, z);
        public static float YawFromSource(float ry) => -ry;
    }
}
