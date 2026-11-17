// Small sprites for the web style HUD, drawn once at run time (no texture files): the slanted HEAT bars
// (style.css #stars i: 9 x 16, skewed 18 degrees, lit with a #ffb04a to #e8401c gradient), round dots and
// the radar mask, and the player arrow.
using UnityEngine;

namespace StreetEmpire.UI
{
    public static class HudArt
    {
        static Sprite barOn, barOff, circle, arrow;

        static Sprite Make(Texture2D t) { t.wrapMode = TextureWrapMode.Clamp; t.Apply(); return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f); }

        public static Sprite Bar(bool on)
        {
            if (on && barOn != null) return barOn; if (!on && barOff != null) return barOff;
            const int W = 28, H = 48; float skew = Mathf.Tan(18f * Mathf.Deg2Rad);
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false);
            Color top = new Color32(0xff, 0xb0, 0x4a, 0xff), bottom = new Color32(0xe8, 0x40, 0x1c, 0xff);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float x0 = (y - H / 2f) * skew + W / 2f - 9f, x1 = x0 + 18f;   // a slanted 18 px wide bar
                    bool inside = x >= x0 && x < x1, edge = inside && (x < x0 + 2 || x >= x1 - 2 || y < 2 || y >= H - 2);
                    Color c = !inside ? Color.clear
                        : on ? (edge ? new Color(0, 0, 0, 1) : Color.Lerp(bottom, top, y / (float)H))
                        : (edge ? new Color(1, 1, 1, 0.7f) : new Color(0, 0, 0, 0.6f));
                    t.SetPixel(x, y, c);
                }
            var sp = Make(t); if (on) barOn = sp; else barOff = sp; return sp;
        }

        public static Sprite Circle()
        {
            if (circle != null) return circle;
            const int N = 128; var t = new Texture2D(N, N, TextureFormat.RGBA32, true);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(N / 2f, N / 2f));
                    t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(N / 2f - d)));
                }
            return circle = Make(t);
        }

        public static Sprite Arrow()
        {
            if (arrow != null) return arrow;
            const int N = 64; var t = new Texture2D(N, N, TextureFormat.RGBA32, true);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float half = (N - 8 - y) * 0.42f;   // a triangle pointing up, its base at the bottom
                    bool inside = y >= 4 && y < N - 4 && Mathf.Abs(x - N / 2f) < half;
                    t.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            return arrow = Make(t);
        }
    }
}
