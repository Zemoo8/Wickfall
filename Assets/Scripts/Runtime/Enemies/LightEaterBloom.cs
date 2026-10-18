using UnityEngine;
using UnityEngine.Rendering.Universal;
using Wickfall.Combat;
using Wickfall.DevTools;
using Wickfall.Meta;
using Wickfall.Presentation;

namespace Wickfall.Enemies
{
    /// <summary>
    /// PDF §2.2 Bois-Mèche: "Fleurs mange-lumière (lumière coupée 3 s)". A rooted flower with a pale
    /// core. When the player's light washes over it, it snaps open and swallows the light for 3 s.
    /// A trimmed wick lights less ground, so you can slip past. It can be cut down (kill reward).
    /// </summary>
    [RequireComponent(typeof(Damageable))]
    public sealed class LightEaterBloom : MonoBehaviour, IDebugEntity
    {
        public EnemyTypeSO type;
        public HazardSO hazard;
        public SpriteRenderer sprite;
        public SpriteAnimator animator;
        public Sprite[] pulse = new Sprite[0];
        public Sprite[] feed = new Sprite[0];
        public Light2D core;
        public float reach = 0.4f;
        public string entityId = "";
        public Wickfall.World.WaxPickup pickupPrefab;

        Damageable body;
        StateMachine<LightEaterBloom> fsm;
        State<LightEaterBloom> idle, tell, cooldown, dead;
        int entityHash;

        public string DebugName => "Bloom";
        public string DebugState => fsm != null ? fsm.CurrentName : "-";
        public bool IsFeeding => fsm != null && fsm.Is(tell);

        void Awake()
        {
            body = GetComponent<Damageable>();
            fsm = new StateMachine<LightEaterBloom>(this);
            idle = fsm.Add(new Idle());
            tell = fsm.Add(new Tell());
            cooldown = fsm.Add(new Cooldown());
            dead = fsm.Add(new Dead());
            body.Died += OnDied;
        }

        void OnEnable() => DevOverlay.Entities.Add(this);
        void OnDisable() => DevOverlay.Entities.Remove(this);

        void Start()
        {
            if (hazard == null) hazard = Db.Hazard(HazardKind.LightEaterBloom);
            if (type == null) type = Db.Enemy(EnemyKind.LightEaterBloom);
            body.maxHealth = type.maxHealth;
            body.ResetHealth();
            entityHash = StableHash.Of(entityId);
            if (!string.IsNullOrEmpty(entityId) && GameSession.Instance != null && GameSession.Instance.HasFlag("dead/" + entityId))
            {
                gameObject.SetActive(false);
                return;
            }
            if (!PresentationMode.IsEnabled(Feature.Hazards))
            {
                gameObject.SetActive(false);
                return;
            }
            fsm.Change(idle);
        }

        void Update() => fsm.Tick(Time.deltaTime);

        /// <summary>True when the player's light reaches the flower.</summary>
        bool Lit(out Player.PlayerController player)
        {
            var session = GameSession.Instance;
            player = session != null ? session.player : null;
            if (player == null || !player.isActiveAndEnabled || player.IsExtinguished || !Events.LightChanged.HasLast)
                return false;
            var light = Events.LightChanged.Last;
            float d = Vector2.Distance(light.Position, transform.position);
            return d <= light.Radius * 0.75f + reach;
        }

        void OnDied(HitData hit)
        {
            if (!string.IsNullOrEmpty(entityId))
                GameSession.Instance?.SetFlag("dead/" + entityId);
            int wax = CombatMath.KillReward(type, Random.value);
            Wickfall.World.WaxPickup.Burst(pickupPrefab, transform.position, wax, type.shardDrop);
            Events.EnemyKilled.Raise(new EnemyKilledInfo { Kind = EnemyKind.LightEaterBloom, Position = transform.position, WaxReward = wax, EntityId = entityHash });
            Events.Flare.Raise(new FlareInfo(transform.position, Db.Balance.killNoiseRadius, FlareKind.KillNoise, Color.white));
            Vfx.Play(VfxId.DeathPuff, transform.position);
            fsm.Change(dead);
        }

        void Show(Sprite[] frames, float fps, bool loop)
        {
            if (animator != null && frames != null && frames.Length > 0)
                animator.Play(frames, fps, loop, true);
        }

        sealed class Idle : State<LightEaterBloom>
        {
            public override string Name => "Idle";
            public override void Enter() => Owner.Show(Owner.pulse, 4f, true);
            public override void Tick(float dt)
            {
                var o = Owner;
                if (o.core != null)
                    o.core.intensity = 0.35f + Mathf.Sin(Time.time * 2.5f) * 0.12f;
                if (o.Lit(out _))
                    o.fsm.Change(o.tell);
            }
        }

        sealed class Tell : State<LightEaterBloom>
        {
            bool swallowed;
            public override string Name => "Feeding";
            public override void Enter()
            {
                swallowed = false;
                Owner.Show(Owner.feed, 10f, false);
                Events.Play(SoundId.BloomSnap, Owner.transform.position);
            }

            public override void Tick(float dt)
            {
                var o = Owner;
                if (!swallowed && o.fsm.TimeInState >= o.hazard.telegraphSeconds)
                {
                    swallowed = true;
                    if (o.Lit(out var player))
                    {
                        // PDF: "lumière coupée 3 s"
                        var hit = HitData.FromHazard(0f, o.transform.position, 0f);
                        hit.Effects = HitEffects.LightCut;
                        hit.StatusSeconds = o.hazard.lightEffectSeconds;
                        player.Body.TakeHit(hit);
                        if (o.core != null) o.core.intensity = 1.6f;
                        Vfx.Play(VfxId.VoidBurst, player.transform.position + new Vector3(0f, 1.6f, 0f));
                        Events.Shake(0.08f, 0.2f);
                    }
                }
                if (o.fsm.TimeInState >= 0.9f)
                    o.fsm.Change(o.cooldown);
            }
        }

        sealed class Cooldown : State<LightEaterBloom>
        {
            public override string Name => "Digesting";
            public override void Enter() => Owner.Show(Owner.pulse, 2f, true);
            public override void Tick(float dt)
            {
                if (Owner.core != null)
                    Owner.core.intensity = Mathf.MoveTowards(Owner.core.intensity, 0.35f, dt);
                if (Owner.fsm.TimeInState >= Owner.hazard.period)
                    Owner.fsm.Change(Owner.idle);
            }
        }

        sealed class Dead : State<LightEaterBloom>
        {
            public override string Name => "Dead";
            public override void Enter()
            {
                foreach (var c in Owner.GetComponentsInChildren<Collider2D>())
                    c.enabled = false;
                if (Owner.core != null) Owner.core.enabled = false;
            }

            public override void Tick(float dt)
            {
                if (Owner.sprite != null)
                {
                    var c = Owner.sprite.color;
                    c.a = Mathf.Max(0f, 1f - Owner.fsm.TimeInState / 0.8f);
                    Owner.sprite.color = c;
                }
                if (Owner.fsm.TimeInState > 0.9f)
                    Owner.gameObject.SetActive(false);
            }
        }
    }
}
