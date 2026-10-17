using UnityEngine;

namespace Wickfall
{
    /// <summary>Player numbers the PDF leaves open (G05, G06). Every value is a tunable proposal.</summary>
    [CreateAssetMenu(menuName = "WICKFALL/Config/Balance", fileName = "Balance")]
    public sealed class BalanceSO : ScriptableObject
    {
        [Header("Movement")]
        public float moveSpeed = 4.6f;
        public float acceleration = 60f;
        public float deceleration = 70f;
        public float interactRadius = 1.1f;

        [Header("Attack")]
        public float baseDamage = 10f;
        public float hitStopSeconds = 0.05f;
        public float knockback = 6f;

        [Header("Dodge (G05)")]
        public int dodgeCharges = 1;
        public float dodgeDistance = 2.5f;
        public float dodgeSeconds = 0.18f;
        public float dodgeInvulnerableSeconds = 0.18f;
        public float dodgeRechargeSeconds = 0.9f;

        [Header("Hurt")]
        public float hurtSeconds = 0.22f;
        public float invulnerableAfterHurt = 0.7f;
        public float hurtKnockback = 5f;

        [Header("Death (PDF: la flamme s'éteint, retour au dernier brasero)")]
        public float extinguishSeconds = 2.2f;

        [Header("Flare (PDF: changer de flamme provoque un éclat qui attire les ennemis)")]
        public float flareRadius = 9f;
        [Tooltip("Killing makes noise too (PDF §2.3: \"plus de lumière et plus de bruit\").")]
        public float killNoiseRadius = 5f;

        [Header("Pickups")]
        public float pickupMagnetRadius = 2.2f;
        public float pickupMagnetSpeed = 9f;
    }
}
