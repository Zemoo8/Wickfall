using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Wickfall.Combat;
using Wickfall.Presentation;

namespace Wickfall.World
{
    /// <summary>
    /// PDF §2.2 Fonderies de Suie: "Presses (35 dégâts)". A press head hangs over a striped plate. It
    /// rests up, then warns (it shudders, its shadow grows, a red-hot edge glows, a hiss: every telegraph
    /// is audible), slams for 35, and rises again. Between slams you walk under it.
    /// </summary>
    public sealed class Press : MonoBehaviour
    {
        public HazardSO hazard;
        public Transform head;
        public SpriteRenderer headSprite;
        public Sprite headCold;
        public Sprite headHot;
        public SpriteRenderer shadow;
        public Light2D warningLight;
        public Hitbox hitbox;
        [Tooltip("Height of the raised head above the plate, in units.")]
        public float raisedHeight = 1.2f;
        [Tooltip("Seconds added to the cycle so neighbouring presses do not slam together.")]
        public float phaseOffset;
        public float riseSeconds = 0.5f;

        public enum Phase { Up, Warning, Slam, Rising }
        public Phase Current { get; private set; }
        float timer;

        public bool IsWarning => Current == Phase.Warning;
        public bool IsSlamming => Current == Phase.Slam;

        static readonly List<Press> all = new List<Press>(16);
        public static IReadOnlyList<Press> All => all;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        /// <summary>Seconds until the head next lands (0 while it is down).</summary>
        public float SecondsUntilSlam
        {
            get
            {
                if (hazard == null)
                    return float.MaxValue;
                switch (Current)
                {
                    case Phase.Up: return Mathf.Max(0f, UpSeconds - timer) + hazard.telegraphSeconds;
                    case Phase.Warning: return Mathf.Max(0f, hazard.telegraphSeconds - timer);
                    case Phase.Slam: return 0f;
                    default: return Mathf.Max(0f, riseSeconds - timer) + UpSeconds + hazard.telegraphSeconds;
                }
            }
        }

        /// <summary>True while the head is down or still on its way up (do not step under it).</summary>
        public bool IsDownOrRising => Current == Phase.Slam || Current == Phase.Rising;

        float UpSeconds => Mathf.Max(0.4f, hazard.period - hazard.telegraphSeconds - hazard.activeSeconds - riseSeconds);

        void Start()
        {
            if (hazard == null)
                hazard = Db.Hazard(HazardKind.Press);
            if (hitbox != null)
                hitbox.radius = hazard.radius;
            timer = -phaseOffset;
            Enter(Phase.Up);
        }

        void Update()
        {
            if (!PresentationMode.IsEnabled(Feature.Hazards))
                return;
            timer += Time.deltaTime;
            switch (Current)
            {
                case Phase.Up:
                    SetHeight(1f, 0f);
                    if (timer >= UpSeconds)
                        Enter(Phase.Warning);
                    break;
                case Phase.Warning:
                {
                    float t = Mathf.Clamp01(timer / hazard.telegraphSeconds);
                    SetHeight(1f + 0.08f * t, t);
                    if (head != null)
                        head.localPosition += new Vector3(Mathf.Sin(Time.time * 70f) * 0.03f * t, 0f, 0f); // shudder
                    if (warningLight != null)
                        warningLight.intensity = Mathf.Lerp(0.1f, 1.1f, t);
                    if (timer >= hazard.telegraphSeconds)
                        Enter(Phase.Slam);
                    break;
                }
                case Phase.Slam:
                    SetHeight(0f, 1f);
                    if (timer >= hazard.activeSeconds)
                        Enter(Phase.Rising);
                    break;
                case Phase.Rising:
                {
                    float t = Mathf.Clamp01(timer / riseSeconds);
                    SetHeight(t, 1f - t);
                    if (timer >= riseSeconds)
                        Enter(Phase.Up);
                    break;
                }
            }
        }

        void Enter(Phase next)
        {
            Current = next;
            timer = next == Phase.Up && timer < 0f ? timer : 0f;
            if (warningLight != null)
                warningLight.enabled = next == Phase.Warning || next == Phase.Slam;
            if (headSprite != null)
                headSprite.sprite = next == Phase.Warning || next == Phase.Slam ? (headHot != null ? headHot : headCold) : headCold;
            switch (next)
            {
                case Phase.Warning:
                    Events.Play(SoundId.PressWarn, transform.position);
                    break;
                case Phase.Slam:
                    Events.Play(SoundId.PressSlam, transform.position);
                    Vfx.Play(VfxId.DustPuff, transform.position);
                    Events.Shake(0.12f, 0.18f);
                    if (hitbox != null) // PDF: 35 dégâts
                        hitbox.Activate(HitData.FromHazard(hazard.damage, transform.position, 6f), Vector2.down, hazard.activeSeconds);
                    break;
            }
        }

        /// <summary>height: 1 = raised, 0 = down on the plate; shade: how dark and wide the shadow is.</summary>
        void SetHeight(float height, float shade)
        {
            if (head != null)
                head.localPosition = new Vector3(0f, raisedHeight * height, 0f);
            if (shadow != null)
            {
                var c = shadow.color;
                c.a = Mathf.Lerp(0.25f, 0.75f, shade);
                shadow.color = c;
                float s = Mathf.Lerp(0.7f, 1f, shade);
                shadow.transform.localScale = new Vector3(s, s, 1f);
            }
        }
    }
}
