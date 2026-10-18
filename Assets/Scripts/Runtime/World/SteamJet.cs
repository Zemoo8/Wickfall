using UnityEngine;
using Wickfall.Combat;
using Wickfall.Enemies;
using Wickfall.Meta;
using Wickfall.Presentation;

namespace Wickfall.World
{
    /// <summary>
    /// PDF §2.2 Fonderies de Suie: "jets de vapeur". A floor vent that hisses and wisps (the warning),
    /// then vents a column of steam along its direction: a scald (G06 values) and a strong push that can
    /// carry you onto a conveyor or under a press.
    /// </summary>
    public sealed class SteamJet : MonoBehaviour
    {
        public HazardSO hazard;
        public Vector2 direction = Vector2.up;
        [Tooltip("Cells covered by the jet beyond the vent.")]
        public int length = 3;
        public SpriteRenderer sprite;
        public Sprite idle;
        public Sprite[] warning = new Sprite[0];
        public Sprite[] venting = new Sprite[0];
        public float phaseOffset;

        public enum Phase { Idle, Warning, Venting }
        public Phase Current { get; private set; }
        float timer;
        float frameTime;
        float nextPuff;
        static readonly Collider2D[] buffer = new Collider2D[12];
        readonly IDamageable[] scalded = new IDamageable[12];
        int scaldedCount;
        HitData scald;

        public bool IsVenting => Current == Phase.Venting;

        float IdleSeconds => Mathf.Max(0.4f, hazard.period - hazard.telegraphSeconds - hazard.activeSeconds);

        void Start()
        {
            if (hazard == null)
                hazard = Db.Hazard(HazardKind.SteamJet);
            direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.up;
            timer = -phaseOffset;
            Current = Phase.Idle;
        }

        void Update()
        {
            if (!PresentationMode.IsEnabled(Feature.Hazards))
                return;
            timer += Time.deltaTime;
            frameTime += Time.deltaTime;
            switch (Current)
            {
                case Phase.Idle:
                    if (sprite != null && idle != null) sprite.sprite = idle;
                    if (timer >= IdleSeconds) Enter(Phase.Warning);
                    break;
                case Phase.Warning:
                    Animate(warning, 8f);
                    if (timer >= hazard.telegraphSeconds) Enter(Phase.Venting);
                    break;
                case Phase.Venting:
                    Animate(venting, 12f);
                    if (Time.time >= nextPuff)
                    {
                        nextPuff = Time.time + 0.12f;
                        Vector2 at = (Vector2)transform.position + direction * Random.Range(0.3f, length + 0.5f);
                        Vfx.Play(VfxId.SteamBurst, at, direction);
                    }
                    if (timer >= hazard.activeSeconds) Enter(Phase.Idle);
                    break;
            }
        }

        void FixedUpdate()
        {
            if (Current != Phase.Venting || !PresentationMode.IsEnabled(Feature.Hazards))
                return;
            // Everything standing in the column is scalded once per vent and pushed along the jet (G06).
            Vector2 center = (Vector2)transform.position + direction * (length + 1) * 0.5f;
            Vector2 size = new Vector2(Mathf.Abs(direction.x) > 0.5f ? length + 1f : 0.9f, Mathf.Abs(direction.y) > 0.5f ? length + 1f : 0.9f);
            var filter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = Layers.PlayerMask | Layers.EnemyMask };
            int count = Physics2D.OverlapBox(center, size, 0f, filter, buffer);
            for (int i = 0; i < count; i++)
            {
                var c = buffer[i];
                buffer[i] = null;
                if (c == null)
                    continue;
                var player = c.GetComponentInParent<Player.PlayerController>();
                if (player != null)
                {
                    player.Status.AddPush(direction * hazard.pushSpeed);
                    Scald(player.Body);
                    continue;
                }
                var motor = c.GetComponentInParent<EnemyMotor>();
                if (motor != null)
                {
                    motor.AddPush(direction * hazard.pushSpeed);
                    Scald(motor.GetComponent<Damageable>());
                }
            }
        }

        void Enter(Phase next)
        {
            Current = next;
            timer = 0f;
            frameTime = 0f;
            if (next == Phase.Warning)
                Events.Play(SoundId.SteamHiss, transform.position, 0.5f);
            if (next == Phase.Venting)
            {
                Events.Play(SoundId.SteamHiss, transform.position);
                scald = HitData.FromHazard(hazard.damage, transform.position, 2f);
                scald.Direction = direction;
                for (int i = 0; i < scaldedCount; i++)
                    scalded[i] = null;
                scaldedCount = 0;
            }
        }

        void Scald(IDamageable target)
        {
            if (target == null || !target.IsAlive)
                return;
            for (int i = 0; i < scaldedCount; i++)
                if (scalded[i] == target)
                    return;
            if (scaldedCount < scalded.Length)
                scalded[scaldedCount++] = target;
            target.TakeHit(scald);
        }

        void Animate(Sprite[] frames, float fps)
        {
            if (sprite == null || frames == null || frames.Length == 0)
                return;
            sprite.sprite = frames[Mathf.FloorToInt(frameTime * fps) % frames.Length];
        }
    }
}
