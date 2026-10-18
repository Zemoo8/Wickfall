using UnityEngine;
using Wickfall.Combat;
using Wickfall.Meta;
using Wickfall.Presentation;

namespace Wickfall.Player
{
    /// <summary>
    /// The player's body. A hit removes wax (PDF: "Chaque coup reçu rétrécit ce que vous voyez"),
    /// soot makes hits hurt x1.25 for 1.5 s and dims the light, the Bloom cuts the light for 3 s.
    /// Each hit leaves a wax drop on the ground (PDF §2.3).
    /// </summary>
    public sealed class PlayerDamageable : MonoBehaviour, IDamageable
    {
        PlayerController player;
        WaxSystem wax;
        PlayerStatus status;
        float invulnerableUntil;
        float extraWaxPerHit;

        public Team Team => Team.Player;
        public bool IsAlive => wax != null && !wax.IsExtinguished;
        public DamageTags Tags => DamageTags.None;
        public Vector2 Position => (Vector2)transform.position + new Vector2(0f, 0.4f);
        public bool IsInvulnerable => Time.time < invulnerableUntil || (player != null && player.IsDodgeInvulnerable);

        void Awake()
        {
            player = GetComponent<PlayerController>();
            wax = GetComponent<WaxSystem>();
            status = GetComponent<PlayerStatus>();
            foreach (var hurtbox in GetComponentsInChildren<Hurtbox>(true))
                hurtbox.Bind(this);
        }

        public void ApplyModifiers(in PlayerModifiers modifiers) => extraWaxPerHit = modifiers.ExtraWaxPerHit;

        public void SetInvulnerable(float seconds) => invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds);

        public bool TakeHit(in HitData hit)
        {
            if (!IsAlive || IsInvulnerable)
                return false;

            // Status-only hits (the Bloom's light cut) never hurt.
            if ((hit.Effects & HitEffects.LightCut) != 0)
                status.ApplyLightCut(hit.StatusSeconds);
            if (hit.Amount <= 0f)
                return true;

            var soot = Db.Hazard(HazardKind.Soot);
            float sootMultiplier = soot != null ? soot.damageTakenMultiplier : 1.25f;
            float amount = CombatMath.PlayerIncoming(hit.Amount, status.Sooted, sootMultiplier, extraWaxPerHit);
            float removed = wax.Damage(amount);

            // Soot is applied after the hit, so a soot ball removes exactly the PDF's 10 wax (G28).
            if ((hit.Effects & HitEffects.Soot) != 0)
                status.ApplySoot(soot != null ? soot.lightEffectSeconds : 1.5f);
            if ((hit.Effects & HitEffects.Slow) != 0)
                status.ApplySlow(hit.SlowFactor, hit.SlowSeconds);

            Vector2 position = transform.position;
            Events.PlayerHit.Raise(new HitReport { Position = position, Amount = removed, Victim = Team.Player, Killed = !IsAlive });
            Events.Play(hit.ImpactSound != SoundId.None ? hit.ImpactSound : SoundId.PlayerHurt, position);
            Events.Play(SoundId.PlayerHurt, position, 0.9f);
            if (PresentationMode.IsEnabled(Feature.CameraFeedback))
            {
                Events.Shake(0.22f, 0.18f);
                Events.Feedback.Raise(new FeedbackRequest(FeedbackKind.Flash, 0.35f, 0.12f, new Color(1f, 0.55f, 0.3f)));
            }
            Vfx.Play(VfxId.WaxDrip, position + new Vector2(0f, 0.5f));

            if (IsAlive)
            {
                invulnerableUntil = Time.time + Db.Balance.invulnerableAfterHurt;
                player.OnHurt(hit);
            }
            return true;
        }
    }
}
