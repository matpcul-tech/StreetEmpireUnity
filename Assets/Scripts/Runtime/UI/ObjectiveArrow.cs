// The web game's objective marker (main.js objArrow): a yellow down pointing triangle over the objective with its
// name and distance under it ("UNC 18 m"), held at the screen edge and pointing the way when the objective is
// off screen or behind the camera. Unc while his job is on offer, then the bag, then Unc again.
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

        void Start() { if (arrow) arrow.sprite = HudArt.Arrow(); }

        void LateUpdate()
        {
            var s = GameSession.Instance; var cam = Camera.main;
            bool show = s != null && s.player != null && cam != null && job != null && job.Stage != MissionRunner.State.Passed
                && (opening == null || opening.Stage == MuggerOpening.Beat.Done || opening.Stage == MuggerOpening.Beat.Waiting && opening.Step < 0);
            Vector3 at = Vector3.zero; string name = "";
            if (show)
            {
                if (job.Stage == MissionRunner.State.GetBag) { at = job.Target + Vector3.up * 1.2f; name = "BAG"; }
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
            root.anchoredPosition = p;
        }
    }
}
