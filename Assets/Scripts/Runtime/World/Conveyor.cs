using UnityEngine;
using Wickfall.Enemies;
using Wickfall.Presentation;

namespace Wickfall.World
{
    /// <summary>
    /// PDF §2.2 Fonderies de Suie: "convoyeurs". A belt (one run of conveyor cells) that carries anyone
    /// standing on it at the hazard's speed (G06: 2.4 u/s): walking against it is slow, with it fast.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class Conveyor : MonoBehaviour
    {
        public HazardSO hazard;
        public Vector2 direction = Vector2.right;

        BoxCollider2D area;
        static readonly Collider2D[] buffer = new Collider2D[16];
        float nextLoop;

        public Vector2 Push => direction.normalized * (hazard != null ? hazard.pushSpeed : 2.4f);

        void Awake() => area = GetComponent<BoxCollider2D>();

        void Start()
        {
            if (hazard == null)
                hazard = Db.Hazard(HazardKind.Conveyor);
        }

        void FixedUpdate()
        {
            if (!PresentationMode.IsEnabled(Feature.Hazards))
                return;
            Vector2 center = (Vector2)transform.position + area.offset;
            var filter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = Layers.PlayerMask | Layers.EnemyMask };
            int count = Physics2D.OverlapBox(center, area.size - new Vector2(0.2f, 0.2f), 0f, filter, buffer);
            bool playerOn = false;
            for (int i = 0; i < count; i++)
            {
                var c = buffer[i];
                buffer[i] = null;
                if (c == null)
                    continue;
                var player = c.GetComponentInParent<Player.PlayerController>();
                if (player != null)
                {
                    if (!player.IsDodging) // a dodge is a jump over the belt
                        player.Status.AddPush(Push);
                    playerOn = true;
                    continue;
                }
                var motor = c.GetComponentInParent<EnemyMotor>();
                if (motor != null)
                    motor.AddPush(Push);
            }
            if (playerOn && Time.time >= nextLoop)
            {
                nextLoop = Time.time + 1.2f;
                Events.Play(SoundId.ConveyorLoop, transform.position, 0.35f);
            }
        }
    }
}
