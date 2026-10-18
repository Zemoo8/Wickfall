using UnityEngine;
using UnityEngine.Rendering.Universal;
using Wickfall.Combat;
using Wickfall.Presentation;

namespace Wickfall.World
{
    /// <summary>
    /// PDF §2.2 Champs de Suif: "Geysers de cire (10 dégâts, annoncés 1,2 s)". Dormant, then a
    /// glowing, bubbling 1.2 s warning (with a sound cue: every telegraph is audible, prompt §11),
    /// then the eruption. The warning glows so it reads in the dark.
    /// </summary>
    public sealed class WaxGeyser : MonoBehaviour
    {
        public HazardSO hazard;
        public SpriteRenderer sprite;
        public Sprite dormant;
        public Sprite[] warning = new Sprite[0];
        public Sprite[] eruption = new Sprite[0];
        public Light2D warningLight;
        public Hitbox hitbox;
        [Tooltip("Seconds added to the cycle so neighbouring geysers do not fire together.")]
        public float phaseOffset;

        enum Phase { Dormant, Warning, Erupting }
        Phase phase;
        float timer;
        float frameTime;

        public bool IsWarning => phase == Phase.Warning;
        public bool IsErupting => phase == Phase.Erupting;

        void Start()
        {
            if (hazard == null)
                hazard = Db.Hazard(HazardKind.WaxGeyser);
            timer = -phaseOffset;
            if (hitbox != null)
                hitbox.radius = hazard.radius;
            Show(Phase.Dormant);
        }

        float DormantSeconds => Mathf.Max(0.5f, hazard.period - hazard.telegraphSeconds - hazard.activeSeconds);

        void Update()
        {
            if (!PresentationMode.IsEnabled(Feature.Hazards))
                return;
            timer += Time.deltaTime;
            frameTime += Time.deltaTime;
            switch (phase)
            {
                case Phase.Dormant:
                    if (timer >= DormantSeconds)
                    {
                        timer = 0f;
                        Show(Phase.Warning);
                        Events.Play(SoundId.GeyserWarn, transform.position);
                    }
                    break;
                case Phase.Warning:
                    if (warningLight != null)
                        warningLight.intensity = Mathf.Lerp(0.2f, 1.4f, timer / hazard.telegraphSeconds);
                    Animate(warning, 10f);
                    if (timer >= hazard.telegraphSeconds) // PDF: annoncés 1,2 s
                    {
                        timer = 0f;
                        Show(Phase.Erupting);
                        Erupt();
                    }
                    break;
                case Phase.Erupting:
                    Animate(eruption, 14f);
                    if (timer >= hazard.activeSeconds)
                    {
                        timer = 0f;
                        Show(Phase.Dormant);
                    }
                    break;
            }
        }

        void Erupt()
        {
            Events.Play(SoundId.GeyserErupt, transform.position);
            Vfx.Play(VfxId.GeyserBurst, transform.position);
            if (hitbox != null)
                hitbox.Activate(HitData.FromHazard(hazard.damage, transform.position, 5f), Vector2.up, hazard.activeSeconds);
            Events.Shake(0.06f, 0.15f);
        }

        void Show(Phase next)
        {
            phase = next;
            frameTime = 0f;
            if (warningLight != null)
                warningLight.enabled = next != Phase.Dormant;
            if (sprite != null && next == Phase.Dormant && dormant != null)
                sprite.sprite = dormant;
        }

        void Animate(Sprite[] frames, float fps)
        {
            if (sprite == null || frames == null || frames.Length == 0)
                return;
            int i = Mathf.FloorToInt(frameTime * fps) % frames.Length;
            sprite.sprite = frames[i];
        }
    }
}
