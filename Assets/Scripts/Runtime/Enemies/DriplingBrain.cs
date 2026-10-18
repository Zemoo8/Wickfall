using UnityEngine;
using Wickfall.Combat;

namespace Wickfall.Enemies
{
    /// <summary>
    /// PDF §2.2 Champs de Suif: Dripling, a slow melee creature (PDF §3: "a hunched blob of dripping
    /// pale wax"). Cold wax that hungers for light: it oozes toward you and lunges a short way.
    /// </summary>
    public sealed class DriplingBrain : EnemyBrain
    {
        public float lungeSpeed = 4.5f;

        protected override void PerformAttack(Vector2 direction)
        {
            base.PerformAttack(direction);
            Motor.Teleport((Vector2)transform.position + direction * 0.25f);
            Events.Play(SoundId.DriplingSquelch, transform.position);
        }
    }
}
