using UnityEngine;
using Wickfall.Combat;

namespace Wickfall.Enemies
{
    /// <summary>
    /// PDF §2.2 Fonderies de Suie: "Scorifiés (armurés)" — heavy golems of rusted iron and slag, hurt only
    /// by the white or green flame (PDF §2.3; the armor rule lives in CombatMath). Slow, clanking walk;
    /// a long overhead smash that lands in a wide circle in front of it.
    /// </summary>
    public sealed class SlagbornBrain : EnemyBrain
    {
        public float smashRadius = 1.25f;
        float nextStep;

        protected override void Update()
        {
            base.Update();
            if (!IsDead && Anim == EnemyAnim.Move && Time.time >= nextStep)
            {
                nextStep = Time.time + 0.55f;
                Events.Play(SoundId.SlagbornStep, transform.position, 0.6f);
            }
        }

        protected override void PerformAttack(Vector2 direction)
        {
            if (attackHitbox == null)
                return;
            var hit = HitData.FromEnemy(type.attackDamage, transform.position, direction, 6f);
            attackHitbox.offset = direction * 1.0f + new Vector2(0f, 0.3f);
            attackHitbox.radius = smashRadius;
            attackHitbox.Activate(hit, direction, type.attackActive, Body);
            Vector2 impact = (Vector2)transform.position + direction * 1.0f;
            Vfx.Play(VfxId.DustPuff, impact);
            Events.Play(SoundId.SlagbornSmash, impact);
            Events.Shake(0.14f, 0.25f);
        }
    }
}
