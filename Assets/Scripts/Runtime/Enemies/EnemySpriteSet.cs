using UnityEngine;

namespace Wickfall.Enemies
{
    /// <summary>A creature's animations (drawn facing right; the visuals flip them for the left).</summary>
    [CreateAssetMenu(menuName = "WICKFALL/Visuals/Enemy Sprite Set", fileName = "EnemySprites")]
    public sealed class EnemySpriteSet : ScriptableObject
    {
        public SpriteClip idle;
        public SpriteClip move;
        public SpriteClip windup;
        public SpriteClip attack;
        public SpriteClip hurt;
        public SpriteClip death;
        public SpriteClip special;
        public SpriteClip special2;
        [Tooltip("Height of the alert mark above the pivot, in units.")]
        public float alertHeight = 1.6f;

        public SpriteClip For(EnemyAnim anim)
        {
            switch (anim)
            {
                case EnemyAnim.Move: return move.IsValid ? move : idle;
                case EnemyAnim.Windup: return windup.IsValid ? windup : (attack.IsValid ? attack : idle);
                case EnemyAnim.Attack: return attack.IsValid ? attack : idle;
                case EnemyAnim.Hurt: return hurt.IsValid ? hurt : idle;
                case EnemyAnim.Dead: return death.IsValid ? death : idle;
                case EnemyAnim.Special: return special.IsValid ? special : idle;
                default: return idle;
            }
        }
    }
}
