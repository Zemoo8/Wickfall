using System.Collections.Generic;
using UnityEngine;
using Wickfall.Combat;
using Wickfall.DevTools;
using Wickfall.Meta;
using Wickfall.Presentation;
using Wickfall.World;

namespace Wickfall.Enemies
{
    public enum EnemyAnim { Idle, Move, Windup, Attack, Hurt, Dead, Special }

    /// <summary>
    /// PDF §4 EnemyBrain: "Machine à états des ennemis : errance, alerte, poursuite, attaque, retour.
    /// Chaque type ajoute son comportement." Subclasses override the hooks (attack, when they may
    /// move, extra states). Kills give 4 to 15 wax and make noise (PDF §2.3).
    /// </summary>
    [RequireComponent(typeof(Damageable), typeof(EnemyPerception), typeof(EnemyMotor))]
    public abstract class EnemyBrain : MonoBehaviour, IDebugEntity
    {
        public EnemyTypeSO type;
        public string entityId = "";
        public WaxPickup pickupPrefab;
        public Hitbox attackHitbox;

        public Damageable Body { get; private set; }
        public EnemyPerception Perception { get; private set; }
        public EnemyMotor Motor { get; private set; }
        public EnemyAnim Anim { get; protected set; }
        public float AnimSince { get; private set; }
        public bool IsDead { get; private set; }
        public Vector2 Home { get; protected set; }
        public string DebugName => name;
        public string DebugState => Fsm != null ? Fsm.CurrentName : "-";

        protected StateMachine<EnemyBrain> Fsm;
        protected State<EnemyBrain> WanderState, AlertState, ChaseState, AttackState, SearchState, ReturnState, DeadState;

        float nextAttack;
        int entityHash;
        bool noticed;
        bool fought;
        protected Vector2 searchPoint;

        static readonly List<EnemyBrain> all = new List<EnemyBrain>(64);
        public static IReadOnlyList<EnemyBrain> All => all;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        protected virtual void Awake()
        {
            Body = GetComponent<Damageable>();
            Perception = GetComponent<EnemyPerception>();
            Motor = GetComponent<EnemyMotor>();
            Fsm = new StateMachine<EnemyBrain>(this);
            CreateStates();
            Body.Damaged += OnDamaged;
            Body.Died += OnDied;
        }

        protected virtual void CreateStates()
        {
            WanderState = Fsm.Add(new Wander());
            AlertState = Fsm.Add(new Alert());
            ChaseState = Fsm.Add(new Chase());
            AttackState = Fsm.Add(new Attack());
            SearchState = Fsm.Add(new Search());
            ReturnState = Fsm.Add(new Return());
            DeadState = Fsm.Add(new Dead());
        }

        protected virtual void OnEnable()
        {
            all.Add(this);
            DevOverlay.Entities.Add(this);
        }

        protected virtual void OnDisable()
        {
            all.Remove(this);
            DevOverlay.Entities.Remove(this);
        }

        protected virtual void Start()
        {
            Home = transform.position;
            entityHash = string.IsNullOrEmpty(entityId) ? GetInstanceID() : StableHash.Of(entityId);
            if (!string.IsNullOrEmpty(entityId) && GameSession.Instance != null && GameSession.Instance.HasFlag(DeadFlag))
            {
                gameObject.SetActive(false); // killed earlier in this run: the dead stay dead (G08)
                return;
            }
            if (type != null)
            {
                Body.maxHealth = type.maxHealth;
                Body.tags = type.tags;
                Body.knockbackResistance = type.knockbackResistance;
                Body.kind = type.kind;
                Perception.perceptionScale = type.perceptionScale;
                Motor.bodyRadius = type.colliderRadius;
            }
            Body.ResetHealth();
            if (!PresentationMode.IsEnabled(Feature.Enemies))
            {
                gameObject.SetActive(false);
                return;
            }
            Fsm.Change(InitialState);
        }

        string DeadFlag => "dead/" + entityId;

        protected virtual State<EnemyBrain> InitialState => WanderState;

        protected virtual void Update()
        {
            if (IsDead)
            {
                Fsm.Tick(Time.deltaTime);
                return;
            }
            if (!noticed && Perception.InDetectionRange && type != null && type.countsForAvoided)
            {
                noticed = true;
                GameSession.Instance?.Tracker.Noticed(entityHash);
            }
            Fsm.Tick(Time.deltaTime);
        }

        protected void SetAnim(EnemyAnim anim)
        {
            if (Anim == anim)
                return;
            Anim = anim;
            AnimSince = Time.time;
        }

        // ---- hooks -------------------------------------------------------------------------------

        /// <summary>False to freeze in place (the Lurker moves only while you shine).</summary>
        protected virtual bool MayMove => true;
        protected virtual float AttackRange => type.attackRange;
        protected virtual float ChaseSpeed => type.chaseSpeed;
        protected virtual float WanderSpeed => type.wanderSpeed;

        protected virtual bool WantsToAttack(float distance) => distance <= AttackRange && Time.time >= nextAttack;

        /// <summary>The moment the attack lands (melee by default).</summary>
        protected virtual void PerformAttack(Vector2 direction)
        {
            if (attackHitbox == null)
                return;
            var hit = HitData.FromEnemy(type.attackDamage, transform.position, direction);
            attackHitbox.offset = direction * Mathf.Max(0.3f, type.attackRange * 0.5f) + new Vector2(0f, 0.4f);
            attackHitbox.radius = Mathf.Max(0.5f, type.attackRange * 0.6f);
            attackHitbox.Activate(hit, direction, type.attackActive, Body);
        }

        protected virtual void OnAlerted()
        {
            Vfx.Play(VfxId.AlertMark, (Vector2)transform.position + new Vector2(0f, 1.6f));
            Events.Play(SoundId.EnemyAlert, transform.position, 0.7f);
            Events.EnemyAlerted.Raise(new EnemyAlertInfo { Kind = type.kind, Position = transform.position, EntityId = entityHash, Alerted = true });
        }

        // ---- damage and death ----------------------------------------------------------------------

        protected virtual void OnDamaged(HitData hit, float amount)
        {
            if (hit.Source == Team.Player)
            {
                if (!fought)
                {
                    fought = true;
                    GameSession.Instance?.Tracker.Fought(entityHash);
                }
                var session = GameSession.Instance;
                Perception.Reveal(session != null && session.player != null ? (Vector2)session.player.transform.position : hit.Origin);
                if (!IsDead && (Fsm.Is(WanderState) || Fsm.Is(ReturnState) || Fsm.Is(SearchState)))
                    Fsm.Change(ChaseState);
            }
            Vfx.Play(VfxId.HitSpark, (Vector2)transform.position + new Vector2(0f, 0.5f), default,
                hit.HasFlame ? Db.Flame(hit.Flame).bodyColor : Color.white);
            Events.Play(hit.ImpactSound != SoundId.None ? hit.ImpactSound : SoundId.HitWax, transform.position, 0.8f);
        }

        protected virtual void OnDied(HitData hit)
        {
            IsDead = true;
            var session = GameSession.Instance;
            if (!string.IsNullOrEmpty(entityId))
                session?.SetFlag(DeadFlag);
            session?.Tracker.Killed(entityHash);

            int wax = type != null ? CombatMath.KillReward(type, Random.value) : 0;
            int shards = type != null ? type.shardDrop : 0;
            WaxPickup.Burst(pickupPrefab, (Vector2)transform.position + new Vector2(0f, 0.4f), wax, shards);
            Events.EnemyKilled.Raise(new EnemyKilledInfo
            {
                Kind = type != null ? type.kind : EnemyKind.Dripling, Position = transform.position, WaxReward = wax, Shards = shards, EntityId = entityHash,
            });
            // PDF §2.3: a kill means more light and more noise.
            Events.Flare.Raise(new FlareInfo(transform.position, Db.Balance.killNoiseRadius, FlareKind.KillNoise, Color.white));
            Events.Play(SoundId.EnemyDeath, transform.position);
            Vfx.Play(VfxId.DeathPuff, transform.position);
            Fsm.Change(DeadState);
        }

        // ---- shared states -----------------------------------------------------------------------

        protected sealed class Wander : State<EnemyBrain>
        {
            Vector2 target;
            float pauseUntil;
            public override string Name => "Wander";
            public override void Enter()
            {
                Owner.Motor.Stop();
                pauseUntil = Time.time + Random.Range(0.5f, 2f);
                target = Owner.Home;
            }

            public override void Tick(float dt)
            {
                var o = Owner;
                if (o.Perception.CanSeePlayer) { o.Fsm.Change(o.AlertState); return; }
                if (o.Perception.ConsumeInvestigate(out var point)) { o.searchPoint = point; o.Fsm.Change(o.SearchState); return; }
                if (!o.MayMove || o.WanderSpeed <= 0f) { o.Motor.Stop(); o.SetAnim(EnemyAnim.Idle); return; }
                if (Time.time < pauseUntil) { o.Motor.Stop(); o.SetAnim(EnemyAnim.Idle); return; }
                if (o.Motor.MoveTo(target, o.WanderSpeed) < 0.2f)
                {
                    Vector2 offset = Random.insideUnitCircle * o.type.wanderRadius;
                    target = o.Home + offset;
                    if (NavGrid.Active != null && !NavGrid.Active.IsWalkable(LevelData.CellOf(target)))
                        target = o.Home;
                    pauseUntil = Time.time + Random.Range(0.8f, 2.5f);
                }
                o.SetAnim(EnemyAnim.Move);
            }
        }

        protected sealed class Alert : State<EnemyBrain>
        {
            public override string Name => "Alert";
            public override void Enter()
            {
                Owner.Motor.Stop();
                Owner.SetAnim(EnemyAnim.Idle);
                Owner.OnAlerted();
            }

            public override void Tick(float dt)
            {
                var o = Owner;
                o.Motor.Face(o.Perception.PlayerPosition - (Vector2)o.transform.position);
                if (o.Fsm.TimeInState >= o.type.alertSeconds)
                    o.Fsm.Change(o.Perception.HasLastKnown ? o.ChaseState : o.ReturnState);
            }
        }

        protected sealed class Chase : State<EnemyBrain>
        {
            public override string Name => "Chase";
            public override void Tick(float dt)
            {
                var o = Owner;
                var p = o.Perception;
                Vector2 position = o.transform.position;
                Vector2 target = p.CanSeePlayer ? p.PlayerPosition : p.LastKnown;
                float distance = Vector2.Distance(position, target);

                if ((position - o.Home).sqrMagnitude > o.type.leashRadius * o.type.leashRadius && !p.CanSeePlayer)
                {
                    o.Fsm.Change(o.ReturnState);
                    return;
                }
                if (p.CanSeePlayer && o.WantsToAttack(distance))
                {
                    o.Fsm.Change(o.AttackState);
                    return;
                }
                if (!p.CanSeePlayer && p.TimeSinceSeen > o.type.loseSightSeconds)
                {
                    o.searchPoint = p.LastKnown;
                    o.Fsm.Change(o.SearchState);
                    return;
                }
                if (!o.MayMove)
                {
                    o.Motor.Stop();
                    o.SetAnim(EnemyAnim.Special);
                    return;
                }
                if (distance > o.AttackRange * 0.8f)
                {
                    o.Motor.MoveTo(target, o.ChaseSpeed);
                    o.SetAnim(EnemyAnim.Move);
                }
                else
                {
                    o.Motor.Stop();
                    o.Motor.Face(target - position);
                    o.SetAnim(EnemyAnim.Idle);
                }
            }
        }

        protected sealed class Attack : State<EnemyBrain>
        {
            Vector2 direction;
            bool landed;
            public override string Name => "Attack";
            public override void Enter()
            {
                var o = Owner;
                o.Motor.Stop();
                direction = (o.Perception.PlayerPosition - (Vector2)o.transform.position).normalized;
                o.Motor.Face(direction);
                landed = false;
                o.SetAnim(EnemyAnim.Windup);
            }

            public override void Tick(float dt)
            {
                var o = Owner;
                float t = o.Fsm.TimeInState;
                if (!landed && t >= o.type.attackWindup)
                {
                    landed = true;
                    o.SetAnim(EnemyAnim.Attack);
                    o.PerformAttack(direction);
                }
                if (t >= o.type.attackWindup + o.type.attackActive + o.type.attackRecover)
                {
                    o.nextAttack = Time.time + o.type.attackCooldown;
                    o.Fsm.Change(o.ChaseState);
                }
            }
        }

        protected sealed class Search : State<EnemyBrain>
        {
            public override string Name => "Search";
            public override void Enter() => Owner.SetAnim(EnemyAnim.Move);
            public override void Tick(float dt)
            {
                var o = Owner;
                if (o.Perception.CanSeePlayer) { o.Fsm.Change(o.AlertState); return; }
                if (o.Perception.ConsumeInvestigate(out var point)) o.searchPoint = point;
                if (!o.MayMove) { o.Motor.Stop(); return; }
                float speed = Mathf.Lerp(o.WanderSpeed, o.ChaseSpeed, 0.6f);
                if (o.Motor.MoveTo(o.searchPoint, speed) < 0.6f)
                {
                    o.Motor.Stop();
                    o.SetAnim(EnemyAnim.Idle);
                }
                if (o.Fsm.TimeInState > o.type.searchSeconds + 6f)
                    o.Fsm.Change(o.ReturnState);
            }
        }

        protected sealed class Return : State<EnemyBrain>
        {
            public override string Name => "Return";
            public override void Enter()
            {
                Owner.Perception.Forget();
                Owner.SetAnim(EnemyAnim.Move);
            }

            public override void Tick(float dt)
            {
                var o = Owner;
                if (o.Perception.CanSeePlayer) { o.Fsm.Change(o.AlertState); return; }
                if (o.Perception.ConsumeInvestigate(out var point)) { o.searchPoint = point; o.Fsm.Change(o.SearchState); return; }
                float speed = Mathf.Max(o.WanderSpeed, o.ChaseSpeed * 0.6f);
                if (!o.MayMove || o.Motor.MoveTo(o.Home, speed) < 0.3f)
                {
                    o.Motor.Stop();
                    if (o.MayMove) o.Fsm.Change(o.WanderState);
                }
            }
        }

        protected sealed class Dead : State<EnemyBrain>
        {
            public override string Name => "Dead";
            public override void Enter()
            {
                var o = Owner;
                o.Motor.Stop();
                o.Motor.Frozen = true;
                o.SetAnim(EnemyAnim.Dead);
                foreach (var c in o.GetComponentsInChildren<Collider2D>())
                    c.enabled = false;
            }

            public override void Tick(float dt)
            {
                if (Owner.Fsm.TimeInState > 1.2f)
                    Owner.gameObject.SetActive(false);
            }
        }
    }
}
