using UnityEngine;

namespace Wickfall.Enemies
{
    /// <summary>
    /// PDF §2.2 Bois-Mèche: "Rôdeurs (bougent si vous brillez)" — Lurkers only move while the player
    /// shines. Trim the wick and they freeze where they are (PDF §2.3 "Lumière contre ennemis").
    /// </summary>
    public sealed class LurkerBrain : EnemyBrain
    {
        protected override bool MayMove => Perception.PlayerShining;

        protected override bool WantsToAttack(float distance) => MayMove && base.WantsToAttack(distance);

        protected override void Update()
        {
            base.Update();
            // Frozen pose whenever the player is dim (even while wandering).
            if (!IsDead && !MayMove && Anim != EnemyAnim.Attack)
                SetAnim(EnemyAnim.Special);
        }
    }
}
