using UnityEngine;
using Wickfall.Combat;

namespace Wickfall.World
{
    /// <summary>
    /// A wax effigy for the Light Lab: takes hits, shows the flame rules (armored and glass
    /// variants block the wrong flames), and reforms after a moment.
    /// </summary>
    [RequireComponent(typeof(Damageable))]
    public sealed class TrainingDummy : MonoBehaviour
    {
        public SpriteRenderer sprite;
        public float reformSeconds = 2.5f;

        Damageable body;
        float deadSince = -1f;
        float shake;
        Vector3 home;

        void Awake()
        {
            body = GetComponent<Damageable>();
            body.Damaged += OnDamaged;
            body.Blocked += OnBlocked;
            body.Died += OnDied;
            home = sprite != null ? sprite.transform.localPosition : Vector3.zero;
        }

        void OnDamaged(HitData hit, float amount) => shake = 0.18f;
        void OnBlocked(HitData hit) => shake = 0.08f;

        void OnDied(HitData hit)
        {
            deadSince = Time.time;
            if (sprite != null)
                sprite.enabled = false;
            Vfx.Play(VfxId.DeathPuff, transform.position + new Vector3(0f, 0.6f, 0f));
            Events.Play(SoundId.EnemyDeath, transform.position);
        }

        void Update()
        {
            if (deadSince >= 0f && Time.time - deadSince > reformSeconds)
            {
                deadSince = -1f;
                body.ResetHealth();
                if (sprite != null)
                    sprite.enabled = true;
            }
            if (sprite != null)
            {
                shake = Mathf.MoveTowards(shake, 0f, Time.deltaTime);
                float x = shake > 0f ? Mathf.Round(Mathf.Sin(Time.time * 70f) * 2f) / 16f : 0f;
                sprite.transform.localPosition = home + new Vector3(x, 0f, 0f);
            }
            body.PendingKnockback = Vector2.zero;
        }
    }
}
