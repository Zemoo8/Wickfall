using UnityEngine;
using UnityEngine.Rendering.Universal;
using Wickfall.Combat;
using Wickfall.DevTools;
using Wickfall.Meta;
using Wickfall.Player;
using Wickfall.World;

namespace Wickfall.Enemies
{
    /// <summary>
    /// PDF §4 BossHollowKing: "Trois phases qui retirent chacune un appui (pas de mode faible, puis cire
    /// maximale à 40)" (G13: phase 1 darkens the arena's wax pools). A crowned figure of negative space.
    /// It slams, casts voids, sends ash waves (phase 2+) and pulses negative light (phase 3). After phase 3
    /// it falls and waits: extinguish it or spare it (G12).
    /// </summary>
    [RequireComponent(typeof(Damageable))]
    public sealed class BossHollowKing : MonoBehaviour, IDebugEntity
    {
        public EnemyTypeSO type;
        public BossConfigSO config;
        public Projectile voidPrefab;
        public SpriteRenderer sprite;
        public SpriteAnimator animator;
        public Sprite[] idle1 = new Sprite[0], idle2 = new Sprite[0], idle3 = new Sprite[0];
        public Sprite[] slam = new Sprite[0];
        public Sprite[] fallen = new Sprite[0];
        public Light2D negative;
        public Hitbox slamHitbox;
        public float wakeRadius = 9f;

        public enum Mode { Dormant, Fighting, Shifting, Fallen, Extinguished, Spared }
        public Mode Current { get; private set; }
        public int Phase { get; private set; }
        public bool IsTelegraphingSlam => attack == Attack.Slam && attackTime < config.slamWindup;
        /// <summary>The void volley is about to leave his hands (a dodge window).</summary>
        public bool VoidsComing => attack == Attack.Voids && attackTime >= 0.3f && attackTime < 0.65f;
        public bool AshWaveActive => attack == Attack.AshWave && attackTime > 0.5f;
        public float WaveRadius => waveRadius;
        public Vector2 SlamTarget { get; private set; }

        public string DebugName => "Roi Creux";
        public string DebugState => Current + (Current == Mode.Fighting ? " p" + Phase + " " + attack : "");

        enum Attack { None, Slam, Voids, AshWave, NegativePulse }
        Attack attack;
        float attackTime;
        float nextAttack;
        float shiftUntil;
        float waveRadius;
        bool waveHit;
        float nextTouch;
        int attackCount;
        Damageable body;
        EnemyMotor motor;

        public Damageable Body => body;

        void Awake()
        {
            body = GetComponent<Damageable>();
            motor = GetComponent<EnemyMotor>();
            body.Damaged += OnDamaged;
            body.Died += OnFell;
        }

        void OnEnable()
        {
            DevOverlay.Entities.Add(this);
            Events.PlayerRespawned.Register(OnPlayerRespawned);
        }

        void OnDisable()
        {
            DevOverlay.Entities.Remove(this);
            Events.PlayerRespawned.Unregister(OnPlayerRespawned);
        }

        /// <summary>If the Wickbearer goes out mid-fight, the King gathers himself: the fight starts over.</summary>
        void OnPlayerRespawned()
        {
            if (Current != Mode.Fighting && Current != Mode.Shifting)
                return;
            Current = Mode.Dormant;
            Phase = 0;
            attack = Attack.None;
            body.ResetHealth();
            if (negative != null) negative.enabled = false;
            motor?.Teleport(home);
            Show(idle1, 4f);
        }

        Vector2 home;

        void Start()
        {
            if (type == null) type = Db.Enemy(EnemyKind.HollowKing);
            if (config == null) config = Db.Data.boss;
            body.maxHealth = config.healthPerPhase * 3f;
            body.ResetHealth();
            var session = GameSession.Instance;
            if (session != null && session.Save.kingDecided)
            {
                Current = session.Save.kingSpared ? Mode.Spared : Mode.Extinguished;
                ShowFallen(!session.Save.kingSpared);
                return;
            }
            Current = Mode.Dormant;
            home = transform.position;
            Show(idle1, 4f);
            if (voidPrefab != null)
                Pools.Prewarm(voidPrefab.gameObject, config.voidsPerVolley);
            if (negative != null)
                negative.enabled = false;
        }

        PlayerController Player
        {
            get
            {
                var s = GameSession.Instance;
                return s != null && s.player != null && s.player.isActiveAndEnabled ? s.player : null;
            }
        }

        void Update()
        {
            var player = Player;
            switch (Current)
            {
                case Mode.Dormant:
                    if (player != null && Vector2.Distance(player.transform.position, transform.position) < wakeRadius)
                        Wake();
                    break;
                case Mode.Shifting:
                    if (Time.time >= shiftUntil)
                    {
                        Current = Mode.Fighting;
                        nextAttack = Time.time + 0.6f;
                    }
                    break;
                case Mode.Fighting:
                    Fight(player);
                    break;
            }
        }

        void Wake()
        {
            Current = Mode.Fighting;
            Phase = 1;
            nextAttack = Time.time + 1.2f;
            Events.Play(SoundId.KingRise, transform.position);
            Events.Music.Raise(MusicId.Boss);
            Events.Shake(0.15f, 0.8f);
            Events.BossPhaseChanged.Raise(1); // G13: the arena's wax pools go dark
        }

        float AttackGap => config.timeBetweenAttacks * Mathf.Pow(config.phaseSpeedup, Phase - 1);

        void Fight(PlayerController player)
        {
            if (player == null || player.IsExtinguished)
            {
                motor?.Stop();
                return;
            }
            Vector2 me = transform.position;
            Vector2 target = player.transform.position;
            float distance = Vector2.Distance(me, target);
            Touch(player, distance);

            if (attack == Attack.None)
            {
                // drift toward the Wickbearer between attacks
                if (motor != null)
                {
                    if (distance > 2.2f) motor.MoveTo(target, config.moveSpeed);
                    else motor.Stop();
                }
                Show(PhaseIdle, 5f);
                if (Time.time >= nextAttack)
                    Begin(PickAttack(distance));
                return;
            }

            attackTime += Time.deltaTime;
            motor?.Stop();
            switch (attack)
            {
                case Attack.Slam:
                    if (attackTime >= config.slamWindup && !slamDone)
                    {
                        slamDone = true;
                        var hit = HitData.FromEnemy(config.slamDamage, SlamTarget, (target - SlamTarget).normalized, 7f);
                        if (slamHitbox != null)
                        {
                            slamHitbox.transform.position = SlamTarget;
                            slamHitbox.radius = config.slamRadius * 0.5f;
                            slamHitbox.Activate(hit, Vector2.down, 0.15f, body);
                        }
                        Vfx.Play(VfxId.DustPuff, SlamTarget);
                        Vfx.Play(VfxId.AshWave, SlamTarget);
                        Events.Play(SoundId.KingSlam, SlamTarget);
                        Events.Shake(0.25f, 0.35f);
                    }
                    if (attackTime >= config.slamWindup + 0.7f) End();
                    break;
                case Attack.Voids:
                    if (attackTime >= 0.6f && !voidsDone)
                    {
                        voidsDone = true;
                        FireVoids(target);
                    }
                    if (attackTime >= 1.2f) End();
                    break;
                case Attack.AshWave:
                    waveRadius = Mathf.Max(0f, attackTime - 0.5f) * 6f;
                    if (!waveHit && attackTime > 0.5f && Mathf.Abs(distance - waveRadius) < 0.6f && !player.IsDodging)
                    {
                        waveHit = true;
                        var hit = HitData.FromEnemy(config.ashWaveDamage, me, (target - me).normalized, 3f);
                        if (player.Body.TakeHit(hit))
                            player.Status.ApplySlow(config.ashSlowMultiplier, 2f); // "Cendre qui ralentit"
                    }
                    if (attackTime >= 0.5f + 8f / 6f) End();
                    break;
                case Attack.NegativePulse:
                    // negative light: inside it your flame is swallowed (PDF: "Foyer en lumière négative")
                    if (negative != null)
                    {
                        negative.enabled = true;
                        negative.intensity = Mathf.Min(1f, attackTime * 2f) * 0.8f;
                    }
                    if (distance < config.negativePulseRadius)
                        player.Status.ApplyLightCut(0.3f);
                    if (attackTime >= config.negativePulseSeconds)
                    {
                        if (negative != null) negative.enabled = false;
                        End();
                    }
                    break;
            }
        }

        bool slamDone, voidsDone;
        Sprite[] PhaseIdle => Phase >= 3 && idle3.Length > 0 ? idle3 : Phase == 2 && idle2.Length > 0 ? idle2 : idle1;

        Attack PickAttack(float distance)
        {
            attackCount++;
            if (Phase >= 3 && attackCount % 4 == 0) return Attack.NegativePulse;
            if (Phase >= 2 && attackCount % 3 == 0) return Attack.AshWave;
            return distance < config.slamRadius + 1f ? Attack.Slam : Attack.Voids;
        }

        void Begin(Attack next)
        {
            attack = next;
            attackTime = 0f;
            slamDone = voidsDone = waveHit = false;
            var player = Player;
            SlamTarget = player != null ? (Vector2)player.transform.position : (Vector2)transform.position;
            switch (next)
            {
                case Attack.Slam:
                    Show(slam, slam.Length / Mathf.Max(0.3f, config.slamWindup + 0.4f), false);
                    break;
                case Attack.Voids:
                    Events.Play(SoundId.KingVoid, transform.position);
                    break;
                case Attack.AshWave:
                    Events.Play(SoundId.KingSlam, transform.position, 0.6f);
                    Vfx.Play(VfxId.AshWave, transform.position);
                    break;
                case Attack.NegativePulse:
                    Events.Play(SoundId.KingVoid, transform.position, 1f);
                    break;
            }
        }

        void End()
        {
            attack = Attack.None;
            nextAttack = Time.time + AttackGap;
        }

        void FireVoids(Vector2 target)
        {
            if (voidPrefab == null)
                return;
            Vector2 origin = (Vector2)transform.position + new Vector2(0f, 2.2f);
            Vector2 aim = (target - origin).normalized;
            int n = config.voidsPerVolley;
            for (int i = 0; i < n; i++)
            {
                float spread = (i - (n - 1) / 2f) * 14f;
                Vector2 dir = Quaternion.Euler(0f, 0f, spread) * aim;
                var hit = HitData.FromEnemy(config.voidDamage, origin, dir, 2f);
                hit.ImpactSound = SoundId.KingVoid;
                var p = Pools.Spawn(voidPrefab, origin, Quaternion.identity);
                p.Launch(hit, dir, config.voidSpeed, 14f);
            }
        }

        void Touch(PlayerController player, float distance)
        {
            if (Time.time < nextTouch || distance > 1.2f) // only real contact (his body keeps you ~1.4 away)
                return;
            nextTouch = Time.time + 1f;
            player.Body.TakeHit(HitData.FromEnemy(config.contactDamage, transform.position, ((Vector2)player.transform.position - (Vector2)transform.position).normalized, 5f));
        }

        void OnDamaged(HitData hit, float amount)
        {
            Vfx.Play(VfxId.HitSpark, (Vector2)transform.position + new Vector2(0f, 2f), default,
                hit.HasFlame ? Db.Flame(hit.Flame).bodyColor : Color.white);
            if (Current == Mode.Dormant)
                Wake();
            int phaseNow = 1 + Mathf.FloorToInt((body.maxHealth - body.Health) / config.healthPerPhase);
            if (body.IsAlive && phaseNow > Phase && Phase < 3)
                ShiftTo(Mathf.Min(3, phaseNow));
        }

        void ShiftTo(int phase)
        {
            Phase = phase;
            Current = Mode.Shifting;
            shiftUntil = Time.time + 1.5f;
            attack = Attack.None;
            body.SetInvulnerable(1.5f);
            if (negative != null) negative.enabled = false;
            Events.Play(SoundId.KingPhase, transform.position);
            Vfx.Play(VfxId.VoidBurst, (Vector2)transform.position + new Vector2(0f, 2f));
            Events.Shake(0.2f, 0.7f);
            Events.BossPhaseChanged.Raise(phase); // phase 2: no trim; phase 3: max wax 40 (PDF)
        }

        void OnFell(HitData hit)
        {
            Current = Mode.Fallen;
            attack = Attack.None;
            motor?.Stop();
            if (negative != null) negative.enabled = false;
            foreach (var c in GetComponentsInChildren<Collider2D>())
                if (!c.isTrigger) c.enabled = false;
            ShowFallen(false);
            Events.Play(SoundId.KingFall, transform.position);
            Events.Shake(0.3f, 1.2f);
            Events.BossPhaseChanged.Raise(0); // the supports come back
            var interact = GetComponentInChildren<KingJudgement>(true);
            if (interact != null)
                interact.gameObject.SetActive(true);
        }

        /// <summary>G12: the player decides the fallen King's fate.</summary>
        public void Decide(bool spare)
        {
            if (Current != Mode.Fallen)
                return;
            Current = spare ? Mode.Spared : Mode.Extinguished;
            if (!spare)
            {
                Vfx.Play(VfxId.ExtinguishBurst, (Vector2)transform.position + new Vector2(0f, 1.5f));
                Events.Play(SoundId.Extinguish, transform.position);
            }
            ShowFallen(!spare);
            GameSession.Instance?.DecideKing(spare);
        }

        void ShowFallen(bool gone)
        {
            if (animator != null && fallen.Length > 0)
                animator.Play(fallen, 3f, true, true);
            if (sprite != null)
                sprite.color = gone ? new Color(1f, 1f, 1f, 0.25f) : Color.white;
        }

        void Show(Sprite[] frames, float fps, bool loop = true)
        {
            if (animator == null || frames == null || frames.Length == 0)
                return;
            if (animator.CurrentFrames == frames)
                return;
            animator.Play(frames, fps, loop, true);
        }
    }
}
