using UnityEngine;
using UnityEngine.Rendering.Universal;
using Wickfall.Combat;

namespace Wickfall.World
{
    /// <summary>One panel of the Rose: it accepts only its own flame (PDF §2.3), and only while the Rose is powered.</summary>
    public sealed class RosePanel : MonoBehaviour, IDamageable
    {
        public FlameId flame;
        public RoseWindow rose;
        public SpriteRenderer sprite;
        public Light2D glow;

        public bool IsLit { get; private set; }

        public Team Team => Team.Neutral;
        public bool IsAlive => true;
        public DamageTags Tags => DamageTags.None;
        public Vector2 Position => transform.position;

        void Awake()
        {
            foreach (var hurtbox in GetComponentsInChildren<Hurtbox>(true))
                hurtbox.Bind(this);
            SetLit(false, false);
        }

        public bool TakeHit(in HitData hit)
        {
            if (IsLit || hit.Source != Team.Player || !hit.HasFlame)
                return false;
            if (hit.Flame != flame || rose == null || !rose.Powered)
            {
                // the wrong flame (or a sleeping Rose): the glass rings but stays dark
                Events.EnemyHit.Raise(new HitReport { Position = Position, Amount = 0f, Blocked = true, Flame = hit.Flame, Victim = Team.Neutral });
                Events.Play(SoundId.HitGlass, Position, 0.6f);
                return false;
            }
            SetLit(true, true);
            rose.PanelLit();
            return true;
        }

        public void SetLit(bool lit, bool announce)
        {
            IsLit = lit;
            var color = Db.Data != null ? Db.Flame(flame).bodyColor : Color.white;
            if (sprite != null)
                sprite.color = lit ? color : new Color(0.35f, 0.33f, 0.3f, 1f);
            if (glow != null)
            {
                glow.enabled = lit;
                glow.color = Db.Data != null ? Db.Flame(flame).lightColor : Color.white;
            }
            if (announce)
            {
                Events.Play(SoundId.PanelLit, Position);
                Vfx.Play(VfxId.Flare, Position, Vector2.up, color);
            }
        }
    }
}
