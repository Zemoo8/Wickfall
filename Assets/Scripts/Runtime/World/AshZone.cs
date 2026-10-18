using UnityEngine;
using Wickfall.Meta;
using Wickfall.Presentation;

namespace Wickfall.World
{
    /// <summary>PDF §2.2 Chambre du Foyer: "Cendre qui ralentit" — a drift of ash that slows whoever wades through it.</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class AshZone : MonoBehaviour
    {
        public HazardSO hazard;

        BoxCollider2D area;
        bool playerInside;

        void Awake() => area = GetComponent<BoxCollider2D>();

        void Start()
        {
            if (hazard == null)
                hazard = Db.Hazard(HazardKind.Ash);
        }

        void OnDisable() => Leave();

        void FixedUpdate()
        {
            var session = GameSession.Instance;
            var player = session != null ? session.player : null;
            bool inside = false;
            if (player != null && player.isActiveAndEnabled && PresentationMode.IsEnabled(Feature.Hazards))
            {
                Vector2 feet = player.transform.position;
                Vector2 center = (Vector2)transform.position + area.offset;
                Vector2 half = area.size * 0.5f;
                inside = Mathf.Abs(feet.x - center.x) <= half.x && Mathf.Abs(feet.y - center.y) <= half.y;
            }
            if (inside && !playerInside)
            {
                playerInside = true;
                player.Status.EnterAsh(hazard.moveMultiplier); // G06: x0.55
            }
            else if (!inside && playerInside)
            {
                Leave();
            }
        }

        void Leave()
        {
            if (!playerInside)
                return;
            playerInside = false;
            var session = GameSession.Instance;
            if (session != null && session.player != null)
                session.player.Status.ExitAsh();
        }
    }
}
