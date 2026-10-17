using UnityEngine;

namespace Wickfall
{
    /// <summary>
    /// Tuning for one creature of PDF §2.2. Behaviours come from the PDF; the numbers the PDF does
    /// not give (health, damage, speeds) are proposals logged in Docs/BALANCE.md (G06).
    /// </summary>
    [CreateAssetMenu(menuName = "WICKFALL/Enemy Type", fileName = "Enemy")]
    public sealed class EnemyTypeSO : ScriptableObject
    {
        public EnemyKind kind;
        public string nameKey = "enemy.dripling";
        public RegionId homeRegion;

        [Header("Body")]
        public float maxHealth = 20f;
        public DamageTags tags;
        public float colliderRadius = 0.45f;
        [Range(0f, 1f)] public float knockbackResistance = 0f;

        [Header("Movement")]
        public float wanderSpeed = 0.9f;
        public float chaseSpeed = 1.8f;
        public float wanderRadius = 2.5f;
        [Tooltip("Gives up and goes home past this distance from its spawn point.")]
        public float leashRadius = 12f;

        [Header("Perception (detection radius comes from the player's light, PDF §2.3)")]
        [Tooltip("Scales the player's detection radius for this creature (1 = the PDF formula as is).")]
        public float perceptionScale = 1f;
        public float alertSeconds = 0.45f;
        [Tooltip("Seconds without seeing the player before it searches the last known position.")]
        public float loseSightSeconds = 2.5f;
        public float searchSeconds = 2.5f;

        [Header("Attack")]
        public float contactDamage = 0f;
        public float attackDamage = 8f;
        public float attackRange = 1.1f;
        public float attackWindup = 0.45f;
        public float attackActive = 0.15f;
        public float attackRecover = 0.6f;
        public float attackCooldown = 0.9f;
        public float projectileSpeed = 6f;

        [Header("PDF §2.3 - Tuer un ennemi : gain de 4 à 15 de cire")]
        public int killWaxMin = 4;
        public int killWaxMax = 6;
        public int shardDrop = 2;

        [Header("Score (PDF §2.4: +20 par ennemi évité sans combat)")]
        public bool countsForAvoided = true;
    }
}
