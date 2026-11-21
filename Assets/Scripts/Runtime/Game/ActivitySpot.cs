// Town life: one place where a townie can do something. A door to walk in and out of, a place a worker
// stands to work, or a place a customer stands (a gas pump, the wait by the bay). TownLifeBuilder writes the
// spots into the Eastside scene from each building's footprint; Townie picks an open, free one, walks to
// it, uses it for a while and picks another. The spot's forward is the way its user faces.
using StreetEmpire.City;
using UnityEngine;

namespace StreetEmpire.Game
{
    public enum SpotKind { Door, Work, Visit }

    public class ActivitySpot : MonoBehaviour
    {
        public SpotKind kind;
        public string label = "";
        /// <summary>The building it belongs to, and whether that building is a business with opening hours.</summary>
        public BuildingMarker building;
        public bool shop;
        public int blockId;
        public Vector3 blockCenter;
        /// <summary>How long one use lasts (for a door: how long they stay inside), shortest to longest.</summary>
        public Vector2 seconds = new(10f, 20f);
        /// <summary>Game clock hours it can be used (GameClock.Hour). 0 to 24 is always.</summary>
        public float openHour, closeHour = 24f;
        public int capacity = 1;
        /// <summary>Animator trigger set when a townie starts using the spot ("Work", "PumpGas"). Nothing
        /// happens until Player.controller has a trigger of that name, so clips can be added later.</summary>
        public string animTrigger = "";
        /// <summary>Doors: the point at the door itself, behind the building's box collider. A townie walks
        /// from the spot to it with its capsule off, then goes out of sight.</summary>
        public bool hasEnter;
        public Vector3 enterPoint;

        public int Users { get; private set; }
        public bool Free => Users < capacity;
        public Vector3 Stand => transform.position;
        public bool OpenAt(float hour) => IsOpen(openHour, closeHour, hour);
        public void Take() => Users++;
        public void Leave() => Users = Mathf.Max(0, Users - 1);
        public float Duration() => Random.Range(seconds.x, seconds.y);

        /// <summary>Open from `open` up to `close`; hours that run past midnight (20 to 4) wrap.</summary>
        public static bool IsOpen(float open, float close, float hour)
        {
            if (close - open >= 24f) return true;
            hour = Mathf.Repeat(hour, 24f); open = Mathf.Repeat(open, 24f); close = Mathf.Repeat(close, 24f);
            return open <= close ? hour >= open && hour < close : hour >= open || hour < close;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = kind == SpotKind.Door ? Color.cyan : kind == SpotKind.Work ? new Color(1f, 0.6f, 0.1f) : Color.green;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.9f, 0.3f);
            Gizmos.DrawLine(transform.position + Vector3.up * 0.9f, transform.position + Vector3.up * 0.9f + transform.forward * 0.8f);
            if (hasEnter) Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, enterPoint + Vector3.up * 0.1f);
        }
    }
}
