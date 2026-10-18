using UnityEngine;
using Wickfall.Presentation;

namespace Wickfall.Player
{
    /// <summary>
    /// PDF §4 DimController: "Active ou coupe le mode « tailler la mèche » et applique ses
    /// multiplicateurs." The multipliers (radius -65 %, loss -75 %, detection -60 %, damage -50 %,
    /// no flame arts) live in DimConfig; the other systems apply them when TrimChanged says so.
    /// The Roi Creux's phase 2 takes this mode away ("pas de mode faible").
    /// </summary>
    public sealed class DimController : MonoBehaviour
    {
        public bool IsTrimmed { get; private set; }
        public bool LockedByBoss { get; private set; }
        public float Blend { get; private set; } // 0 = full flame, 1 = fully trimmed (eased, for visuals)

        void OnEnable() => Events.BossPhaseChanged.Register(OnBossPhase);
        void OnDisable() => Events.BossPhaseChanged.Unregister(OnBossPhase);

        void Start() => Events.TrimChanged.Raise(IsTrimmed);

        void OnBossPhase(int phase)
        {
            LockedByBoss = ProgressRules.TrimDisabled(phase, Db.Boss);
            if (LockedByBoss)
                SetTrimmed(false);
        }

        public bool CanTrim => !LockedByBoss && PresentationMode.IsEnabled(Feature.Trim);

        /// <summary>Called each frame with the held state of the trim key (or the toggle).</summary>
        public void Request(bool wantTrimmed) => SetTrimmed(wantTrimmed && CanTrim);

        public void SetTrimmed(bool value)
        {
            if (value == IsTrimmed)
                return;
            IsTrimmed = value;
            Events.TrimChanged.Raise(value);
            Events.Play(value ? SoundId.TrimOn : SoundId.TrimOff, transform.position, 0.7f);
        }

        void Update()
        {
            float target = IsTrimmed ? 1f : 0f;
            float speed = 1f / Mathf.Max(0.01f, Db.Dim.transitionSeconds);
            Blend = Mathf.MoveTowards(Blend, target, speed * Time.deltaTime);
        }

        public float DamageMultiplier => IsTrimmed ? Db.Dim.damageMultiplier : 1f;
        public bool ArtsAllowed => !IsTrimmed || Db.Dim.flameArtsAllowed;
    }
}
