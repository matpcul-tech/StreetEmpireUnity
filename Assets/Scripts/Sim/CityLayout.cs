// City layout from state.js and city.js: districts, street lines, the four corner lots per block and
// which way each kit building faces. Pure math, so tests can compare it with the JS.
using System;
using System.Collections.Generic;
using StreetEmpire.Sim.Data;

namespace StreetEmpire.Sim
{
    public struct BuildingSpot
    {
        public int blockId, index;   // index 0..3 inside the block, in JS order
        public string kind;          // store, house, apt, tower, shed
        public float x, z;           // lot center, source space
        public float w, d, h;        // footprint and the painted box height from the JS
        public int sx, sz;           // which corner of the block (-1 or 1)
        public float yaw;            // source space radians, the street it faces (city.js)
        public int variantSeed;      // kitBuilding(kind, seed): pick = seed % variants
    }

    public static class CityLayout
    {
        public const int Eastside = 0, Downtown = 1, Harbor = 2, Midtown = 3;

        /// <summary>districtOf(b) in state.js.</summary>
        public static int DistrictOf(int gx, int gz, int grid)
        {
            float e = (gx + gz) / (2f * (grid - 1));
            return e <= 0.25f ? Eastside : e <= 0.44f ? Midtown : e <= 0.69f ? Downtown : Harbor;
        }

        /// <summary>lineCoord(i): street centerlines, i = 0..grid.</summary>
        public static float LineCoord(WorldFile w, int i) => (i - w.grid / 2f) * w.pitch;

        /// <summary>buildingsFor(b) in state.js plus the facing rule from city.js buildBlock().</summary>
        public static List<BuildingSpot> BuildingsFor(WorldFile w, Block b)
        {
            var (cx, cz) = Simulation.BlockCenter(w, b);
            float h = w.block / 2f, lot = 13f;
            int d = DistrictOf(b.gx, b.gz, w.grid), i = 0;
            var list = new List<BuildingSpot>(4);
            foreach (int sx in new[] { -1, 1 })
                foreach (int sz in new[] { -1, 1 })
                {
                    int r = Mod(b.id * 13 + sx * 3 + sz * 5 + 20, 4);
                    string kind; float height;
                    if (d == Eastside) { kind = i == 0 ? "store" : i == 3 && b.id % 2 == 1 ? "apt" : "house"; height = kind == "apt" ? 11 : kind == "store" ? 6 : 6.5f; }
                    else if (d == Midtown) { kind = i == 0 ? "store" : r == 0 ? "house" : "apt"; height = kind == "apt" ? 11 + r : kind == "store" ? 6 : 6.5f; }
                    else if (d == Downtown) { kind = r == 0 ? "apt" : "tower"; height = kind == "apt" ? 18 + r * 2 : 22 + r * 9; }
                    else { kind = "shed"; height = 9 + r * 1.2f; }
                    bool alongX = (b.id + i) % 2 == 0;
                    float yaw = alongX ? (sx > 0 ? MathF.PI / 2 : -MathF.PI / 2) : (sz > 0 ? 0 : MathF.PI);
                    list.Add(new BuildingSpot
                    {
                        blockId = b.id, index = i, kind = kind,
                        x = cx + sx * (h - lot / 2), z = cz + sz * (h - lot / 2), w = lot, d = lot, h = height,
                        sx = sx, sz = sz, yaw = yaw, variantSeed = b.id * 7 + i,
                    });
                    i++;
                }
            return list;
        }

        static int Mod(int a, int m) => ((a % m) + m) % m;
    }
}
