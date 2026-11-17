// The round radar from the web game (main.js drawRadar): 80 m out from the player to the rim, turned with the
// camera so straight ahead is up. Dark green ground (#2a332b), grey streets 14 m wide with dashed yellow center
// lines, blocks tinted by crew (Locals #aaa at 35%, yours #ff8a1a at 60% with a light edge) and the capture
// ring in each; people as crew dots, officers and cruisers flashing red and blue, Unc as a big yellow dot
// (and his letter at the rim when he is off the map) while his job is on offer, the job marker as a yellow dot
// held at the rim, the player as a white arrow, and N on the rim.
using System.Collections.Generic;
using StreetEmpire.Game;
using UnityEngine;
using UnityEngine.UI;

namespace StreetEmpire.UI
{
    public class Minimap : MonoBehaviour
    {
        [Header("Baked by GameBuilder")]
        public Vector2 worldMin, worldSize;          // the map texture covers this (Unity x and z)
        public float[] streetX, streetZ; public Vector2 spanX, spanZ;
        public Vector3[] blocks; public bool[] yours; public float blockSize = 40f;
        public Transform unc; public MissionRunner job;
        [Header("Parts")]
        public RectTransform frame, map; public RawImage mapImage; public Image arrow, frameImage; public Text north, uncLetter;
        public float rimMeters = 80f;

        readonly List<Image> dots = new(); readonly List<Health> people = new();
        float scanT; int used;
        static readonly Color Ground = new Color32(0x2a, 0x33, 0x2b, 255), Road = new Color32(0x7a, 0x7a, 0x7a, 255),
            Locals = new Color32(0xaa, 0xaa, 0xaa, 255), You = new Color32(0xff, 0x8a, 0x1a, 255), Edge = new Color32(0xff, 0xc0, 0x78, 255),
            Gold = new Color32(0xff, 0xcc, 0x33, 255), CopRed = new Color32(0xff, 0x22, 0x22, 255), CopBlue = new Color32(0x22, 0x66, 0xff, 255);

        void Start()
        {
            if (frameImage) frameImage.sprite = HudArt.Circle();
            if (arrow) arrow.sprite = HudArt.Arrow();
            mapImage.texture = Draw(512);
        }

        float PxPerMeter => frame.rect.width / 2f / rimMeters;

        Texture2D Draw(int n)
        {
            var px = new Color32[n * n];
            float mpp = worldSize.x / n;
            Vector2 W(int x, int y) => worldMin + new Vector2((x + 0.5f) * mpp, (y + 0.5f) * mpp);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    var p = W(x, y); Color c = Ground;
                    for (int i = 0; blocks != null && i < blocks.Length; i++)
                    {
                        var d = new Vector2(Mathf.Abs(p.x - blocks[i].x), Mathf.Abs(p.y - blocks[i].z)); float h = blockSize / 2f;
                        if (d.x > h || d.y > h) continue;
                        bool mine = yours != null && i < yours.Length && yours[i];
                        c = Color.Lerp(Ground, mine ? You : Locals, mine ? 0.6f : 0.35f);
                        if (h - Mathf.Max(d.x, d.y) < 1.6f) c = mine ? Edge : Color.Lerp(c, Color.black, 0.6f);
                        float ring = Mathf.Abs(d.magnitude - 5f);
                        if (ring < 0.9f) c = mine ? You : Locals;
                    }
                    bool road = false, line = false;
                    foreach (var sx in streetX) if (Mathf.Abs(p.x - sx) < 7f && p.y > spanZ.x - 7f && p.y < spanZ.y + 7f) { road = true; line |= Mathf.Abs(p.x - sx) < 0.6f && Mathf.Repeat(p.y, 8f) < 4f; }
                    foreach (var sz in streetZ) if (Mathf.Abs(p.y - sz) < 7f && p.x > spanX.x - 7f && p.x < spanX.y + 7f) { road = true; line |= Mathf.Abs(p.y - sz) < 0.6f && Mathf.Repeat(p.x, 8f) < 4f; }
                    if (road) c = line ? Color.Lerp(Road, new Color(1f, 0.82f, 0.31f), 0.55f) : Road;
                    px[y * n + x] = c;
                }
            var t = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            t.SetPixels32(px); t.Apply(true, true);
            return t;
        }

        Image Dot()
        {
            if (used < dots.Count) { var d0 = dots[used++]; d0.enabled = true; return d0; }
            var go = new GameObject("Dot", typeof(RectTransform)); var r = (RectTransform)go.transform; r.SetParent(frame, false);
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<Image>(); img.sprite = HudArt.Circle(); img.raycastTarget = false;
            dots.Add(img); used++; return img;
        }

        /// <summary>A world point on the radar (pixels from its center), straight ahead of the camera up.</summary>
        Vector2 ToRadar(Vector3 world, Vector3 player, float yaw)
        {
            var d = new Vector2(world.x - player.x, world.z - player.z) * PxPerMeter;
            float a = yaw * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            return new Vector2(d.x * c - d.y * s, d.x * s + d.y * c);
        }

        void Put(Vector2 at, Color col, float size, bool clampToRim, bool outline)
        {
            float rim = frame.rect.width / 2f - 14f;
            if (at.magnitude > rim) { if (!clampToRim) return; at = at.normalized * rim; }
            if (outline) { var o = Dot(); o.color = Color.black; o.rectTransform.anchoredPosition = at; o.rectTransform.sizeDelta = Vector2.one * (size + 4f); o.transform.SetAsLastSibling(); }
            var d = Dot(); d.color = col; d.rectTransform.anchoredPosition = at; d.rectTransform.sizeDelta = Vector2.one * size; d.transform.SetAsLastSibling();
        }

        void LateUpdate()
        {
            var s = GameSession.Instance; var cam = Camera.main;
            if (s == null || s.player == null || cam == null) return;
            var p = s.player.transform.position; float yaw = cam.transform.eulerAngles.y;
            // the map turns about the player
            map.sizeDelta = worldSize * PxPerMeter;
            map.pivot = new Vector2((p.x - worldMin.x) / worldSize.x, (p.z - worldMin.y) / worldSize.y);
            map.anchoredPosition = Vector2.zero; map.localEulerAngles = new Vector3(0, 0, yaw);

            for (int i = 0; i < used; i++) dots[i].enabled = false;
            used = 0;
            if ((scanT -= Time.deltaTime) <= 0)
            {
                scanT = 0.5f; people.Clear();
                foreach (var h in FindObjectsByType<Health>(FindObjectsSortMode.None)) if (h.gameObject != s.player.gameObject) people.Add(h);
            }
            bool flash = Mathf.FloorToInt(Time.time / 0.2f) % 2 == 0;
            foreach (var h in people)
            {
                if (h == null || h.Down) continue;
                bool cop = h.GetComponent<Cop>() != null;
                Put(ToRadar(h.transform.position, p, yaw), cop ? (flash ? CopRed : CopBlue) : Locals, cop ? 10f : 8f, false, false);
            }
            foreach (var c in s.cruisers) if (c != null) Put(ToRadar(c.transform.position, p, yaw), flash ? CopBlue : CopRed, 13f, false, false);
            bool offer = job != null && job.Stage == MissionRunner.State.Offered;
            if (unc != null && offer) Put(ToRadar(unc.position, p, yaw), Gold, 20f, false, true);
            if (job != null && (job.Stage == MissionRunner.State.GetBag || job.Stage == MissionRunner.State.ReturnBag))
                Put(ToRadar(job.Target, p, yaw), Gold, 26f, true, true);
            float rim = frame.rect.width / 2f - 14f;
            if (uncLetter)
            {
                var u = unc != null ? ToRadar(unc.position, p, yaw) : Vector2.zero;
                uncLetter.enabled = offer && u.magnitude > rim;
                if (uncLetter.enabled) uncLetter.rectTransform.anchoredPosition = u.normalized * rim;
            }
            if (north) north.rectTransform.anchoredPosition = ToRadar(p + Vector3.back * 1000f, p, yaw).normalized * (rim + 2f);
            if (arrow) { arrow.rectTransform.localEulerAngles = new Vector3(0, 0, yaw - s.player.transform.eulerAngles.y); arrow.transform.SetAsLastSibling(); }
        }
    }
}
