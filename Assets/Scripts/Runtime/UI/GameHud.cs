// Top of the screen: cash, wanted stars, the job objective with its clock, and the big message line
// (job briefs, BUSTED, Job done).
using StreetEmpire.Game;
using UnityEngine;
using UnityEngine.UI;

namespace StreetEmpire.UI
{
    public class GameHud : MonoBehaviour
    {
        public Text cash, objective, message;
        public Image[] stars;
        public MissionRunner mission;
        static readonly Color On = new(1f, 0.82f, 0.2f, 1f), Off = new(1f, 1f, 1f, 0.18f);

        void Update()
        {
            var s = GameSession.Instance; if (s == null) return;
            if (cash) cash.text = $"${Mathf.FloorToInt(s.cash)}";
            int st = s.Stars;
            for (int i = 0; i < stars.Length; i++) if (stars[i]) stars[i].color = i < st ? On : Off;
            if (objective) objective.text = mission != null ? mission.Objective : "";
            if (message)
            {
                message.text = s.Message;
                message.color = s.Message.StartsWith("BUSTED") || s.Message.StartsWith("WASTED") ? new Color(1f, 0.3f, 0.25f) : Color.white;
            }
        }
    }
}
