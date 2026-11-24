// The web game's objective marker (main.js objArrow): a yellow down pointing triangle over the objective with its
// name and distance under it ("UNC 18 m"), held at the screen edge and pointing the way when the objective is
// off screen or behind the camera. Unc while his job is on offer, then the bag, then Unc again.
// v74: the marker never draws over the HUD corners: it is pushed out of the top right stats panel and the top left
// radar and objective text (whichever way is shorter, down or toward the middle).
using StreetEmpire.Game;
using UnityEngine;
using UnityEngine.UI;

namespace StreetEmpire.UI
{
    public class ObjectiveArrow : MonoBehaviour
    {
        public MissionRunner job; public MuggerOpening opening;
        public RectTransform canvas, root; public Image arrow; public Text label;
        public float edge = 70f, hideWithin = 2.5f;
        /// <summary>Marker extent around its anchor (the 220 px label and the triangle): half width, below, above.</summary>
        public Vector3 extent = new(112f, 33f, 31f);
        /// <summary>Keep-out boxes in canvas pixels from a top corner: x = width, y = height, z = 1 for the right corner.</summary>
        public Vector3[] keepOut = { new(480f, 252f, 1f), new(340f, 340f, 0f), new(790f, 445f, 0f) };

        void Start() { if (arrow) arrow.sprite = HudArt.Arrow(); }

        void LateUpdate()
        {
            var s = GameSession.Instance; var cam = Camera.main;
            bool show = s != null && s.player != null && cam != null && job != null && job.Stage != MissionRunner.State.Passed
                && (opening == null || opening.Stage == MuggerOpening.Beat.Done || opening.Stage == MuggerOpening.Beat.Waiting && opening.Step < 0);
            Vector3 at = Vector3.zero; string name = "";
            if (show)
            {
                if (job.Stage == MissionRunner.State.GetBag) { at = job.Target + Vector3.up * 1.2f; name = job.TargetName; }
                else { at = job.giver.position + Vector3.up * 2.3f; name = job.contact != null ? job.contact.name.ToUpperInvariant() : "UNC"; }
                float d = Vector2.Distance(new Vector2(at.x, at.z), new Vector2(s.player.transform.position.x, s.player.transform.position.z));
                show = d > hideWithin;
                label.text = $"{name} {Mathf.RoundToInt(d)} m";
            }
            root.gameObject.SetActive(show);
            if (!show) return;
            var sp = cam.WorldToScreenPoint(at);
            bool behind = sp.z < 0; if (behind) sp = -sp;
            float sx = canvas.rect.width / Screen.width, sy = canvas.rect.height / Screen.height;
            var p = new Vector2(sp.x * sx - canvas.rect.width / 2f, sp.y * sy - canvas.rect.height / 2f);
            var half = new Vector2(canvas.rect.width / 2f - edge, canvas.rect.height / 2f - edge);
            bool off = behind || Mathf.Abs(p.x) > half.x || Mathf.Abs(p.y) > half.y;
            if (off)
            {
                if (behind && p.sqrMagnitude < 1f) p = Vector2.down;
                float k = Mathf.Min(half.x / Mathf.Max(1e-3f, Mathf.Abs(p.x)), half.y / Mathf.Max(1e-3f, Mathf.Abs(p.y)));
                p *= k;
                // the triangle points out toward the objective
                arrow.rectTransform.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg - 90f);
            }
            else arrow.rectTransform.localEulerAngles = new Vector3(0, 0, 180f);   // straight down onto it
            root.anchoredPosition = ClampOutOfHud(p, canvas.rect.size);
        }

        /// <summary>Moves the marker anchored at p (canvas center origin) out of every keep-out corner box.</summary>
        public Vector2 ClampOutOfHud(Vector2 p, Vector2 size)
        {
            float W = size.x / 2f, H = size.y / 2f;
            for (int pass = 0; pass < 2; pass++)
                foreach (var k in keepOut)
                {
                    // the box in center origin coordinates
                    float x0 = k.z > 0.5f ? W - k.x : -W, x1 = k.z > 0.5f ? W : -W + k.x, y0 = H - k.y, y1 = H;
                    float mx0 = p.x - extent.x, mx1 = p.x + extent.x, my0 = p.y - extent.y, my1 = p.y + extent.z;
                    if (mx1 <= x0 || mx0 >= x1 || my1 <= y0 || my0 >= y1) continue;
                    float down = my1 - y0;                                   // move below the box
                    float side = k.z > 0.5f ? mx1 - x0 : x1 - mx0;           // move toward the middle
                    if (down <= side) p.y -= down; else p.x += k.z > 0.5f ? -side : side;
                }
            return p;
        }
    }
}
