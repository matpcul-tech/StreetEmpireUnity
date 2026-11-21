// Town life: a person who lives or works on a block. One loop covers all of them: pick an activity spot
// that is open and free, walk to it round the block's sidewalk, use it (stand at the pump, work at the bay,
// go in the door and come back out later), then pick another. Workers keep to their shop's work spots while
// it is open and go inside when it closes. Hit one and it runs, like a pedestrian (Ped).
using System.Collections.Generic;
using UnityEngine;

namespace StreetEmpire.Game
{
    [RequireComponent(typeof(NpcBody), typeof(Health))]
    public class Townie : MonoBehaviour
    {
        public enum Role { Resident, Worker }
        public enum Mode { Choose, Walk, Enter, Inside, Exit, Use, Flee, Down }

        public Role role;
        public int blockId;
        /// <summary>Resident: the door they live behind. Worker: their shop's door.</summary>
        public ActivitySpot home;
        /// <summary>Worker: the shop's work spots.</summary>
        public ActivitySpot[] work = new ActivitySpot[0];
        public float speed = 1.25f, fleeSpeed = 4.5f;

        public Mode State { get; private set; } = Mode.Choose;
        public ActivitySpot Target { get; private set; }
        /// <summary>Inside a building (out of sight) until this Time.time; TownLife brings it back out.</summary>
        public float InsideUntil { get; private set; }
        /// <summary>A worker whose shop closed: stays inside until it opens again.</summary>
        public bool UntilOpen { get; private set; }
        /// <summary>Meters walked so far (tests and tuning).</summary>
        public float Walked { get; private set; }

        static readonly int SpeedId = Animator.StringToHash("Speed");
        NpcBody body; Health health; Transform from; ActivitySpot last;
        readonly List<Vector3> path = new();
        int step; float timer, stuckT, fleeT, downT, sidestep; bool held; Vector3 vel;
        HashSet<string> triggers;

        void Awake()
        {
            body = GetComponent<NpcBody>(); health = GetComponent<Health>();
            health.Damaged += OnDamaged;
        }

        /// <summary>Starts out of sight behind a door and walks out after `delay` seconds. Called on the
        /// still inactive copy, before TownLife switches it on.</summary>
        public void StartInside(ActivitySpot door, float delay, bool untilOpen)
        {
            Target = door; door.Take(); held = true;
            InsideUntil = Time.time + delay; UntilOpen = untilOpen; State = Mode.Inside;
        }

        void OnDamaged(float amount, GameObject source)
        {
            if (State == Mode.Enter || State == Mode.Exit || State == Mode.Inside) return;
            Release(); Target = null;
            from = source != null ? source.transform : null; fleeT = 8f; State = Mode.Flee;
        }

        void Release()
        {
            if (held && Target != null) Target.Leave();
            held = false;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (health.Down)
            {
                if (State != Mode.Down)
                {
                    Release(); Target = null; body.Controller.enabled = true;
                    State = Mode.Down; downT = 0; body.SetDown(true);
                }
                downT += dt;
                if (downT > 5f) { health.Restore(30); body.SetDown(false); fleeT = 8f; State = Mode.Flee; }
                return;
            }
            switch (State)
            {
                case Mode.Choose: Choose(); break;
                case Mode.Walk: Walk(dt); break;
                case Mode.Use: Use(dt); break;
                case Mode.Enter: if (Glide(Target.enterPoint, dt)) GoInside(); break;
                case Mode.Exit: if (Glide(Target.Stand, dt)) FinishExit(); break;
                case Mode.Flee: Flee(dt); break;
            }
        }

        // ---- picking what to do next ----

        void Choose()
        {
            var life = TownLife.Instance;
            if (life == null) { Idle(3f); return; }
            float hour = GameClock.Hour;
            var pick = role == Role.Worker ? PickWork(hour) : PickVisit(life, hour);
            if (pick == null) { Idle(3f); return; }
            Target = pick; pick.Take(); held = true;
            life.Route(blockId, transform.position, pick.Stand, path);
            step = 0; stuckT = 0; State = Mode.Walk;
        }

        /// <summary>Nothing to do right now: stand where it is for a moment, then look again.</summary>
        void Idle(float seconds) { Target = null; timer = seconds; State = Mode.Use; }

        ActivitySpot PickWork(float hour)
        {
            // closed: in through the shop door until it opens again
            if (home != null && !home.OpenAt(hour)) { UntilOpen = true; return home; }
            // now and then into the shop for a moment
            if (home != null && home != last && home.Free && Random.value < 0.2f) return home;
            ActivitySpot pick = null; int seen = 0;
            foreach (var s in work)
                if (s != null && s != last && s.Free && s.OpenAt(hour) && Random.Range(0, ++seen) == 0) pick = s;
            if (pick == null) foreach (var s in work) if (s != null && s.Free && s.OpenAt(hour)) { pick = s; break; }
            return pick;
        }

        ActivitySpot PickVisit(TownLife life, float hour)
        {
            ActivitySpot pick = null; float total = 0;
            foreach (var s in life.SpotsOn(blockId))
            {
                if (s == null || s == last || s.kind == SpotKind.Work || !s.Free || !s.OpenAt(hour)) continue;
                // the pumps, the wait by the bay and the shop doors draw people; home now and then
                float w = s.kind == SpotKind.Visit ? 3f : s.shop ? 3f : s == home ? 2f : 0.5f;
                total += w;
                if (Random.value * total < w) pick = s;
            }
            return pick;
        }

        // ---- walking there ----

        void Walk(float dt)
        {
            if (step >= path.Count) { Arrive(); return; }
            var pos = transform.position;
            var to = path[step] - pos; to.y = 0;
            bool final = step == path.Count - 1;
            float dist = to.magnitude;
            if (dist < (final ? 0.3f : 0.8f)) { step++; stuckT = 0; return; }
            var dir = to / dist;
            var life = TownLife.Instance;
            bool blocked = life != null && life.SomeoneAhead(this, pos, transform.forward);
            sidestep = Mathf.MoveTowards(sidestep, blocked ? 0.8f : 0f, dt * 1.5f);
            // slow into the last point so it stops on the spot instead of circling it
            float pace = final ? Mathf.Clamp(dist / 0.8f, 0.4f, 1f) : 1f;
            var want = (dir + Vector3.Cross(Vector3.up, dir) * sidestep).normalized * speed * pace;
            vel = Vector3.MoveTowards(vel, want, 4f * dt);
            body.Move(vel, dt);
            Walked += body.Speed * dt;
            // held up by something it cannot walk round (a fence, a parked car): step past it
            stuckT = body.Speed < 0.2f ? stuckT + dt : 0f;
            if (stuckT > 3f)
            {
                body.Teleport(new Vector3(path[step].x, pos.y + 0.05f, path[step].z));
                step++; stuckT = 0; vel = Vector3.zero;
            }
        }

        void Arrive()
        {
            vel = Vector3.zero;
            if (Target == null) { State = Mode.Choose; return; }
            if (Target.kind == SpotKind.Door)
            {
                if (Target.hasEnter) { body.Controller.enabled = false; State = Mode.Enter; }
                else GoInside();
                return;
            }
            timer = Target.Duration(); State = Mode.Use;
            Trigger(Target.animTrigger);
        }

        // ---- using a spot ----

        void Use(float dt)
        {
            body.Move(Vector3.zero, dt);   // gravity and the idle blend
            if (Target != null)
            {
                var f = Target.transform.forward; f.y = 0;
                if (f.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(f), 240f * dt);
            }
            timer -= dt;
            if (timer > 0) return;
            if (Target != null) last = Target;
            Release(); Target = null; State = Mode.Choose;
        }

        void Trigger(string name)
        {
            var a = body.animator;
            if (string.IsNullOrEmpty(name) || a == null || !a.isActiveAndEnabled || a.runtimeAnimatorController == null) return;
            if (triggers == null)
            {
                triggers = new HashSet<string>();
                foreach (var p in a.parameters) if (p.type == AnimatorControllerParameterType.Trigger) triggers.Add(p.name);
            }
            if (triggers.Contains(name)) a.SetTrigger(name);
        }

        // ---- in and out of a door ----

        /// <summary>Walks straight to a point with the capsule off (through the building's box collider to
        /// its door). True once it is there.</summary>
        bool Glide(Vector3 to, float dt)
        {
            var pos = transform.position; to.y = pos.y;
            var d = to - pos;
            if (d.sqrMagnitude < 0.01f) return true;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), body.turnRate * dt);
            transform.position = Vector3.MoveTowards(pos, to, speed * dt);
            Walked += speed * dt;
            var a = body.animator;
            if (a != null && a.isActiveAndEnabled)
            {
                a.SetFloat(SpeedId, body.BlendFor(speed, out float rate));
                a.speed = rate;
            }
            return false;
        }

        void GoInside()
        {
            InsideUntil = Time.time + (Target != null ? Target.Duration() : 10f);
            State = Mode.Inside;
            gameObject.SetActive(false);   // TownLife brings it back out
        }

        /// <summary>TownLife calls this right after switching it back on: out of the door it went in.</summary>
        public void ComeOut()
        {
            UntilOpen = false; vel = Vector3.zero;
            var door = Target;
            if (door == null) { body.Controller.enabled = true; State = Mode.Choose; return; }
            var stand = door.Stand;
            if (door.hasEnter)
            {
                body.Controller.enabled = false;
                transform.position = new Vector3(door.enterPoint.x, stand.y, door.enterPoint.z);
                body.Face(stand);
                State = Mode.Exit;
            }
            else
            {
                body.Teleport(stand + Vector3.up * 0.05f);
                FinishExit();
            }
        }

        void FinishExit()
        {
            body.Controller.enabled = true;
            last = Target; Release(); Target = null; State = Mode.Choose;
        }

        // ---- trouble ----

        void Flee(float dt)
        {
            fleeT -= dt;
            if (fleeT <= 0 || from == null) { vel = Vector3.zero; State = Mode.Choose; return; }
            var away = transform.position - from.position; away.y = 0;
            vel = Vector3.MoveTowards(vel, away.normalized * fleeSpeed, 12f * dt);
            body.Move(vel, dt);
        }
    }
}
