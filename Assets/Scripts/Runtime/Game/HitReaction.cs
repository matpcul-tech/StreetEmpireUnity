// Its own file: Unity only gives a MonoBehaviour a proper script asset when the class and file names match.
// Living in CharacterAnim.cs, HitReaction was saved into the scene sometimes with its animator field and
// sometimes without (32 or 44 bytes, different on every build), which the player cannot read reliably.
using UnityEngine;

namespace StreetEmpire.Game
{
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
