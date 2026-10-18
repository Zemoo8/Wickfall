using UnityEngine;

namespace Wickfall.Player
{
    /// <summary>
    /// Timed effects on the player: soot (PDF §2.3: damage x1.25 and light loss for 1.5 s), the
    /// Light-Eater Bloom's light cut (3 s), slows, ash, conveyor pushes and dark zones under the
    /// canopy ("poches d'obscurité où se cacher").
    /// </summary>
    public sealed class PlayerStatus : MonoBehaviour
    {
        float sootUntil;
        float lightCutUntil;
        float slowUntil;
        float slowFactor = 1f;
        int darkZones;
        int ashZones;
        float ashMultiplier = 1f;
        Vector2 push;
        float pushTime = -1f;
        bool wasSooted, wasCut;

        public bool Sooted => Time.time < sootUntil;
        public bool LightCut => Time.time < lightCutUntil;
        public bool InDarkness => darkZones > 0;
        public bool InAsh => ashZones > 0;
        public float LightCutRemaining => Mathf.Max(0f, lightCutUntil - Time.time);
        public float SootRemaining => Mathf.Max(0f, sootUntil - Time.time);

        public float MoveMultiplier
        {
            get
            {
                float m = Time.time < slowUntil ? slowFactor : 1f;
                if (ashZones > 0) m *= ashMultiplier;
                return m;
            }
        }

        /// <summary>
        /// Pushes from conveyors and steam, kept for this physics step and the next (so it never matters
        /// whose FixedUpdate runs first). Timed in physics steps, not frames: several frames can pass
        /// between two steps.
        /// </summary>
        public Vector2 ExternalVelocity => Time.fixedTime - pushTime <= Time.fixedDeltaTime * 1.01f ? push : Vector2.zero;

        public void ApplySoot(float seconds)
        {
            sootUntil = Mathf.Max(sootUntil, Time.time + seconds);
            Raise(HitEffects.Soot, true, seconds);
        }

        public void ApplyLightCut(float seconds)
        {
            lightCutUntil = Mathf.Max(lightCutUntil, Time.time + seconds);
            Raise(HitEffects.LightCut, true, seconds);
        }

        public void ApplySlow(float factor, float seconds)
        {
            slowFactor = Mathf.Min(Time.time < slowUntil ? slowFactor : 1f, factor);
            slowUntil = Mathf.Max(slowUntil, Time.time + seconds);
        }

        public void ClearSoot() => sootUntil = 0f;

        public void ClearAll()
        {
            sootUntil = lightCutUntil = slowUntil = 0f;
            darkZones = ashZones = 0;
            push = Vector2.zero;
        }

        public void EnterDark() => darkZones++;
        public void ExitDark() => darkZones = Mathf.Max(0, darkZones - 1);

        public void EnterAsh(float multiplier)
        {
            ashZones++;
            ashMultiplier = multiplier;
        }

        public void ExitAsh() => ashZones = Mathf.Max(0, ashZones - 1);

        public void AddPush(Vector2 velocity)
        {
            if (pushTime != Time.fixedTime)
            {
                push = Vector2.zero;
                pushTime = Time.fixedTime;
            }
            push += velocity;
        }

        void Update()
        {
            bool sooted = Sooted;
            if (wasSooted && !sooted)
                Raise(HitEffects.Soot, false, 0f);
            wasSooted = sooted;

            bool cut = LightCut;
            if (wasCut && !cut)
                Raise(HitEffects.LightCut, false, 0f);
            wasCut = cut;
        }

        static void Raise(HitEffects effect, bool active, float seconds) =>
            Events.StatusChanged.Raise(new StatusInfo { Effect = effect, Active = active, Duration = seconds });
    }
}
