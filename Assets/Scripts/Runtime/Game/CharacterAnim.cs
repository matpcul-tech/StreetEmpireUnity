// The fight clips on the shared character controller (Player.controller, used by the player, Unc, officers
// and pedestrians): Punch with PunchKind 0 jab or 1 cross, Hit for a flinch, Down for the knockdown fall.
// HitReaction plays Hit and Down from a body's Health, so every character reacts the same way.
using UnityEngine;

namespace StreetEmpire.Game
{
    public static class CharacterAnim
    {
        public static readonly int PunchId = Animator.StringToHash("Punch"), PunchKindId = Animator.StringToHash("PunchKind"),
            HitId = Animator.StringToHash("Hit"), DownId = Animator.StringToHash("Down");

        static bool Ready(Animator a) => a != null && a.isActiveAndEnabled && a.runtimeAnimatorController != null;

        public static void Punch(Animator a, int kind) { if (!Ready(a)) return; a.SetInteger(PunchKindId, kind); a.SetTrigger(PunchId); }
        public static void Hit(Animator a) { if (Ready(a)) a.SetTrigger(HitId); }
        public static void SetDown(Animator a, bool down) { if (Ready(a)) a.SetBool(DownId, down); }
    }

    /// <summary>Flinch when hurt, fall when knocked out, get up when the hit points come back.</summary>
    [RequireComponent(typeof(Health))]
    public class HitReaction : MonoBehaviour
    {
        public Animator animator;
        Health health; bool down;
        public bool IsDown => down;

        void Awake()
        {
            health = GetComponent<Health>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            health.Damaged += (_, __) => { if (!health.Down) CharacterAnim.Hit(animator); };
        }

        void Update()
        {
            if (health.Down == down) return;
            down = health.Down; CharacterAnim.SetDown(animator, down);
        }
    }
}
