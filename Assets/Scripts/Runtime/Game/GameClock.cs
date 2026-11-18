// The web game's clock (state.js clock(): 17.8 + t * 24 / DAY_SECONDS, DAY_SECONDS 480): a new game starts at
// 17:48 and a full day and night takes 8 minutes. The HUD clock and the sun (DayCycle) both read it.
using UnityEngine;

namespace StreetEmpire.Game
{
    public static class GameClock
    {
        public const float StartHour = 17.8f, DaySeconds = 480f;
        /// <summary>Screenshot set ups hold the clock (at the start hour unless set) so views stay comparable.</summary>
        public static bool Frozen;
        public static float FrozenHour = StartHour;
        public static float Hour => Frozen ? FrozenHour : Mathf.Repeat(StartHour + Time.timeSinceLevelLoad * 24f / DaySeconds, 24f);
    }
}
