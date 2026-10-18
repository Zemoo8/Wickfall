using UnityEngine;
using Wickfall.Combat;
using Wickfall.Presentation;

namespace Wickfall.Enemies
{
    /// <summary>Plays a creature's animation from its brain's state, faces its motion, flashes when hit, fades when dead.</summary>
    public sealed class EnemyVisuals : MonoBehaviour
    {
        public EnemySpriteSet sprites;
        public SpriteRenderer body;
        public SpriteAnimator animator;
        public bool flipToFace = true;
        public float deathFade = 0.6f;

        EnemyBrain brain;
        Damageable damageable;
        MaterialPropertyBlock block;
        float flash;
        EnemyAnim shown = (EnemyAnim)(-1);
        static readonly int FlashId = Shader.PropertyToID("_Flash");

        void Awake()
        {
            brain = GetComponentInParent<EnemyBrain>();
            damageable = GetComponentInParent<Damageable>();
            block = new MaterialPropertyBlock();
            if (damageable != null)
            {
                damageable.Damaged += (hit, amount) => flash = 1f;
                damageable.Blocked += hit => flash = 0.4f;
            }
        }

        void OnEnable()
        {
            shown = (EnemyAnim)(-1);
            if (body != null)
                body.color = Color.white;
        }

        void LateUpdate()
        {
            if (brain == null || sprites == null || animator == null)
                return;
            var anim = brain.Anim;
            if (anim != shown)
            {
                shown = anim;
                var clip = sprites.For(anim);
                bool loop = anim == EnemyAnim.Idle || anim == EnemyAnim.Move || anim == EnemyAnim.Special || anim == EnemyAnim.Windup;
                animator.Play(clip.frames, clip.fps > 0f ? clip.fps : 8f, loop && clip.loop, true);
            }

            if (flipToFace && body != null && Mathf.Abs(brain.Motor.Facing.x) > 0.1f)
                body.flipX = brain.Motor.Facing.x < 0f;

            flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 6f);
            if (body != null)
            {
                body.GetPropertyBlock(block);
                block.SetFloat(FlashId, PresentationMode.IsEnabled(Feature.VFX) ? flash : 0f);
                body.SetPropertyBlock(block);
                if (brain.IsDead)
                {
                    float t = Mathf.Clamp01((Time.time - brain.AnimSince - 0.5f) / deathFade);
                    var c = body.color;
                    c.a = 1f - t;
                    body.color = c;
                }
            }
        }
    }
}
