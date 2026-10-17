using UnityEngine;

namespace Wickfall
{
    /// <summary>Damage rules of PDF §2.1 and §2.3, as pure functions.</summary>
    public static class CombatMath
    {
        /// <summary>PDF §2.1 Cramoisie: "x0,6 à 2,2", more damage at low wax: x2.2 at 0 wax, x0.6 at 100.</summary>
        public static float CrimsonMultiplier(float wax, float pdfMaxWax, float atEmpty = 2.2f, float atFull = 0.6f)
        {
            float t = pdfMaxWax > 0f ? Mathf.Clamp01(wax / pdfMaxWax) : 0f;
            return Mathf.Lerp(atEmpty, atFull, t);
        }

        /// <summary>
        /// Damage the player deals: base x flame "Dégâts" x (0.5 while trimmed, PDF §2.1) x upgrades x pacts.
        /// </summary>
        public static float PlayerOutgoing(float baseDamage, FlameTypeSO flame, float wax, float pdfMaxWax, bool trimmed,
            float trimDamageMultiplier, float otherMultiplier)
        {
            float damage = baseDamage * flame.DamageMultiplierAt(wax, pdfMaxWax) * otherMultiplier;
            return trimmed ? damage * trimDamageMultiplier : damage;
        }

        /// <summary>
        /// PDF §2.3 "Flamme contre matière": Scorifiés are hurt only by the white or green flame,
        /// glass is broken only by the crimson flame. Hits without a flame (hazards, enemies) are not filtered.
        /// </summary>
        public static bool CanHurt(DamageTags targetTags, bool hasFlame, FlameId flame)
        {
            if (!hasFlame)
                return true;
            if ((targetTags & DamageTags.Armored) != 0 && flame != FlameId.White && flame != FlameId.Green)
                return false;
            if ((targetTags & DamageTags.Glass) != 0 && flame != FlameId.Crimson)
                return false;
            return true;
        }

        /// <summary>What a target finally takes from a hit, after its type rules (PDF §4: "la cible applique les multiplicateurs selon le type").</summary>
        public static float TargetTakes(float amount, DamageTags targetTags, bool hasFlame, FlameId flame, float hollowBonus)
        {
            if (!CanHurt(targetTags, hasFlame, flame))
                return 0f;
            if (hasFlame && (targetTags & DamageTags.Hollow) != 0)
                amount *= hollowBonus;
            return amount;
        }

        /// <summary>PDF §2.3 Suie: "Dégâts x1,25" while sooted.</summary>
        public static float PlayerIncoming(float amount, bool sooted, float sootMultiplier, float extraPerHit)
        {
            float taken = sooted ? amount * sootMultiplier : amount;
            return taken > 0f ? taken + extraPerHit : taken;
        }

        /// <summary>A kill reward inside the type's range (PDF §2.3: 4 to 15 wax).</summary>
        public static int KillReward(EnemyTypeSO type, float roll01)
        {
            int min = Mathf.Min(type.killWaxMin, type.killWaxMax);
            int max = Mathf.Max(type.killWaxMin, type.killWaxMax);
            return Mathf.Clamp(min + Mathf.FloorToInt(roll01 * (max - min + 1)), min, max);
        }

        /// <summary>Is a point inside an attack arc?</summary>
        public static bool InArc(Vector2 origin, Vector2 facing, Vector2 point, float range, float arcDegrees)
        {
            Vector2 to = point - origin;
            float sqr = to.sqrMagnitude;
            if (sqr > range * range)
                return false;
            if (arcDegrees >= 359f || sqr < 0.0001f)
                return true;
            return Vector2.Angle(facing, to) <= arcDegrees * 0.5f;
        }
    }
}
