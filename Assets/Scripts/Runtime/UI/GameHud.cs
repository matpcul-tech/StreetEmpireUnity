// The web game's HUD (style.css #hud, main.js updateHud): top right the clock, the weapon, cash in cream,
// HEAT with five slanted bars, the red health bar and "Rep n  Turf a/b"; top left the radar (Minimap) with
// the job objective and the onboarding box under it; the big message line in the middle.
using StreetEmpire.Game;
using UnityEngine;
using UnityEngine.UI;

namespace StreetEmpire.UI
{
    public class GameHud : MonoBehaviour
    {
        public Text cash, objective, message, clock, weapon, sub, heatLabel, onboarding;
        public Image[] stars;
        public Image hpFill;
        public GameObject onboardingBox, messageBox;
        public MissionRunner mission;
        public MuggerOpening opening;
        public int turfOwned = 1, turfTotal = 25;

        void Update()
        {
            var s = GameSession.Instance; if (s == null) return;
            if (cash) cash.text = $"${Mathf.FloorToInt(s.cash)}";
            int st = s.Stars;
            for (int i = 0; i < stars.Length; i++) if (stars[i]) stars[i].sprite = HudArt.Bar(i < st);
            if (heatLabel) heatLabel.color = new Color(0.96f, 0.9f, 0.77f, st > 0 ? 1f : 0.85f);
            if (clock)
            {
                float h = GameClock.Hour;   // the same clock the sun follows (DayCycle)
                clock.text = $"{Mathf.FloorToInt(h):00}:{Mathf.FloorToInt(h % 1f * 60f):00}";
            }
            if (weapon) weapon.text = "FISTS";
            if (hpFill && s.playerHealth) hpFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(s.playerHealth.hp / s.playerHealth.max), 1f);
            if (sub) sub.text = $"Rep {s.rep}   Turf {turfOwned}/{turfTotal}";
            // one Unc prompt: while the onboarding box says "Talk to Unc", the objective line stays empty
            if (objective) objective.text = mission != null && !(opening != null && opening.Step == 1) ? mission.Objective : "";
            if (onboarding)
            {
                string step = opening != null ? opening.StepText : "";
                onboarding.text = step;
                if (onboardingBox) onboardingBox.SetActive(step.Length > 0);
            }
            if (message)
            {
                message.text = s.Message;
                message.color = s.Message.StartsWith("BUSTED") || s.Message.StartsWith("WASTED") ? new Color(1f, 0.3f, 0.25f) : new Color32(0xff, 0xcc, 0x66, 255);
                if (messageBox) messageBox.SetActive(!string.IsNullOrEmpty(s.Message));
            }
        }
    }
}
