using UnityEngine;
using Wickfall.Meta;

namespace Wickfall.Enemies
{
    /// <summary>
    /// PDF §2.2 "Cirelles (inoffensives)" — Waxflies: harmless glowing insects (G20). They drift
    /// around their home, give off a tiny light, and scatter when the player comes close.
    /// </summary>
    public sealed class Cirelle : MonoBehaviour
    {
        public float roamRadius = 2.5f;
        public float speed = 1.2f;
        public float fleeRadius = 2.6f;
        public float fleeSpeed = 4f;
        public SpriteAnimator animator;
        public SpriteRenderer sprite;
        public Sprite[] idleFrames = new Sprite[0];
        public Sprite[] flyFrames = new Sprite[0];

        Vector2 home;
        Vector2 target;
        Vector2 velocity;
        float retargetAt;
        float fleeUntil;
        float seed;

        public bool Fleeing => Time.time < fleeUntil;

        void Start()
        {
            home = transform.position;
            target = home;
            seed = Random.value * 10f;
            if (animator != null && flyFrames.Length > 0)
                animator.Play(flyFrames, 10f, true);
        }

        void Update()
        {
            Vector2 position = transform.position;
            var session = GameSession.Instance;
            var player = session != null ? session.player : null;
            if (player != null && player.isActiveAndEnabled)
            {
                Vector2 p = player.transform.position;
                if ((p - position).sqrMagnitude < fleeRadius * fleeRadius)
                {
                    fleeUntil = Time.time + 1.4f;
                    Vector2 away = (position - p).normalized;
                    target = position + away * 3f;
                }
            }

            if (!Fleeing && Time.time >= retargetAt)
            {
                target = home + Random.insideUnitCircle * roamRadius;
                retargetAt = Time.time + Random.Range(1.5f, 3.5f);
            }

            float s = Fleeing ? fleeSpeed : speed;
            Vector2 desired = (target - position);
            desired = desired.sqrMagnitude > 0.04f ? desired.normalized * s : Vector2.zero;
            velocity = Vector2.Lerp(velocity, desired, 1f - Mathf.Exp(-4f * Time.deltaTime));
            float bob = Mathf.Sin((Time.time + seed) * 5f) * 0.15f;
            transform.position = position + velocity * Time.deltaTime + new Vector2(0f, bob * Time.deltaTime);
            if (sprite != null && Mathf.Abs(velocity.x) > 0.05f)
                sprite.flipX = velocity.x < 0f;

            // drift back home when far away
            if ((position - home).sqrMagnitude > roamRadius * roamRadius * 9f && !Fleeing)
                target = home;
        }
    }
}
