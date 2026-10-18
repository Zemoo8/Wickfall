using UnityEngine;
using Wickfall.Combat;

namespace Wickfall.Enemies
{
    /// <summary>
    /// PDF §2.2 Cathédrale de Verre: "Reflets (copient vos 3 dernières actions)". A mirror-black copy of
    /// the Wickbearer. When it closes in, it plays back your last three actions, oldest first, as its
    /// mirror image would: your swings become its swings, your arts its bursts, your dodges its dodges,
    /// your flame swaps its flares. A player who only ever attacks faces three attacks; one who dodges
    /// faces a slippery copy.
    /// </summary>
    public sealed class ReflectionBrain : EnemyBrain
    {
        public float stepSeconds = 0.45f;
        public float artRadius = 1.7f;
        public float dodgeDistance = 2.4f;

        static readonly ActionRecorder Recorder = new ActionRecorder();
        static int listeners;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Recorder.Clear();
            listeners = 0;
        }

        /// <summary>The player's remembered actions (shared by every Reflection).</summary>
        public static ActionRecorder Memory => Recorder;

        static void OnPlayerActed(PlayerActionInfo action) => Recorder.Record(action);

        protected override void OnEnable()
        {
            base.OnEnable();
            if (listeners++ == 0)
                Events.PlayerActed.Register(OnPlayerActed);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (--listeners == 0)
                Events.PlayerActed.Unregister(OnPlayerActed);
        }

        protected override void CreateStates()
        {
            base.CreateStates();
            AttackState = Fsm.Add(new Mimic());
        }

        protected override void Update()
        {
            base.Update();
            if (!IsDead && Anim == EnemyAnim.Idle && Random.value < Time.deltaTime * 0.6f)
                Events.Play(SoundId.ReflectionGlitch, transform.position, 0.35f);
        }

        Vector2 ToPlayer()
        {
            Vector2 d = Perception.PlayerPosition - (Vector2)transform.position;
            return d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.down;
        }

        void Copy(PlayerActionKind kind, Vector2 recordedDirection)
        {
            Vector2 toPlayer = ToPlayer();
            Motor.Face(toPlayer);
            switch (kind)
            {
                case PlayerActionKind.Art:
                {
                    SetAnim(EnemyAnim.Attack);
                    var hit = HitData.FromEnemy(type.attackDamage * 1.6f, transform.position, toPlayer, 6f);
                    HitQuery.Circle((Vector2)transform.position + new Vector2(0f, 0.4f), artRadius, Layers.PlayerMask, hit, Body);
                    Vfx.Play(VfxId.RadiantPulse, (Vector2)transform.position + new Vector2(0f, 0.4f), default, new Color(0.6f, 0.6f, 0.62f));
                    Events.Play(SoundId.ArtCast, transform.position, 0.8f);
                    break;
                }
                case PlayerActionKind.Dodge:
                {
                    // the mirror image dodges the mirrored way (left becomes right)
                    Vector2 d = recordedDirection.sqrMagnitude > 0.01f ? new Vector2(-recordedDirection.x, recordedDirection.y).normalized : new Vector2(-toPlayer.y, toPlayer.x);
                    Body.SetInvulnerable(0.3f);
                    Motor.Teleport((Vector2)transform.position + d * DodgeReach(d));
                    Vfx.Play(VfxId.DustPuff, transform.position);
                    Events.Play(SoundId.Dodge, transform.position, 0.7f);
                    break;
                }
                case PlayerActionKind.SwitchFlame:
                    Events.Flare.Raise(new FlareInfo(transform.position, Db.Balance.flareRadius, FlareKind.FlameSwitch, new Color(0.8f, 0.8f, 0.85f)));
                    Vfx.Play(VfxId.Flare, (Vector2)transform.position + new Vector2(0f, 1.2f), Vector2.up, new Color(0.8f, 0.8f, 0.85f));
                    Events.Play(SoundId.Flare, transform.position, 0.7f);
                    break;
                default:
                    SetAnim(EnemyAnim.Attack);
                    PerformAttack(toPlayer);
                    Events.Play(SoundId.AttackSwing, transform.position, 0.7f);
                    break;
            }
        }

        /// <summary>How far a dodge can go without passing through a wall.</summary>
        float DodgeReach(Vector2 direction)
        {
            var hit = Physics2D.CircleCast((Vector2)transform.position + new Vector2(0f, 0.3f), 0.3f, direction, dodgeDistance, Layers.WallMask);
            return hit.collider != null ? Mathf.Max(0f, hit.distance - 0.1f) : dodgeDistance;
        }

        /// <summary>Plays back the remembered actions, one every stepSeconds, after a short windup.</summary>
        sealed class Mimic : State<EnemyBrain>
        {
            readonly PlayerActionKind[] kinds = new PlayerActionKind[ActionRecorder.Capacity];
            readonly Vector2[] directions = new Vector2[ActionRecorder.Capacity];
            int count, done;

            public override string Name => "Mimic";

            public override void Enter()
            {
                var o = (ReflectionBrain)Owner;
                o.Motor.Stop();
                o.SetAnim(EnemyAnim.Windup);
                count = Recorder.Count;
                for (int i = 0; i < count; i++)
                {
                    var a = Recorder.Get(i);
                    kinds[i] = a.Kind;
                    directions[i] = a.Direction;
                }
                if (count == 0)
                {
                    kinds[0] = PlayerActionKind.Attack; // nothing to copy yet: a plain strike
                    directions[0] = Vector2.zero;
                    count = 1;
                }
                done = 0;
            }

            public override void Tick(float dt)
            {
                var o = (ReflectionBrain)Owner;
                float t = o.Fsm.TimeInState - o.type.attackWindup;
                while (done < count && t >= done * o.stepSeconds)
                {
                    o.Copy(kinds[done], directions[done]);
                    done++;
                }
                if (done >= count && t >= (count - 1) * o.stepSeconds + o.type.attackRecover)
                {
                    o.BeginCooldown();
                    o.Fsm.Change(o.ChaseState);
                }
            }
        }
    }
}
