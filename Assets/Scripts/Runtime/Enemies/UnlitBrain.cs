using UnityEngine;
using Wickfall.Combat;
using Wickfall.Lighting;
using Wickfall.Meta;

namespace Wickfall.Enemies
{
    /// <summary>
    /// PDF §2.2 Creux Profond: "Les Éteints (dérivent vers la lumière la plus forte)" — holes cut out of
    /// the world that drift toward the brightest light they can feel: your flame, a lit brazier, a flare
    /// (a flame swap, an art, a kill). Lure them away with flares; burn them with the white flame
    /// (anti-Creux). Touching one drains wax.
    /// </summary>
    public sealed class UnlitBrain : EnemyBrain
    {
        public float senseRange = 11f;
        public float lureSeconds = 2.5f;

        Vector2 lurePoint;
        float lureUntil;
        float nextTouch;
        State<EnemyBrain> drift;

        public bool IsLured => Time.time < lureUntil;
        public Vector2 Target { get; private set; }

        protected override void CreateStates()
        {
            base.CreateStates();
            drift = Fsm.Add(new Drift());
        }

        protected override State<EnemyBrain> InitialState => drift;

        protected override void OnEnable()
        {
            base.OnEnable();
            Events.Flare.Register(OnFlare);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Events.Flare.Unregister(OnFlare);
        }

        void OnFlare(FlareInfo flare)
        {
            if (IsDead || ((Vector2)transform.position - flare.Position).sqrMagnitude > senseRange * senseRange * 1.5f)
                return;
            lurePoint = flare.Position;
            lureUntil = Time.time + lureSeconds;
        }

        protected override void OnDamaged(HitData hit, float amount)
        {
            // it has no eyes to alert; it only drifts
            Vfx.Play(VfxId.HitSpark, (Vector2)transform.position + new Vector2(0f, 0.5f), default,
                hit.HasFlame ? Db.Flame(hit.Flame).bodyColor : Color.white);
            Events.Play(hit.ImpactSound != SoundId.None ? hit.ImpactSound : SoundId.HitWax, transform.position, 0.8f);
        }

        /// <summary>The light it is drawn to: a fresh flare, else the strongest source it can feel.</summary>
        bool PickTarget(out Vector2 target)
        {
            if (IsLured)
            {
                target = lurePoint;
                return true;
            }
            if (LightSources.TryStrongest(transform.position, senseRange, out var best))
            {
                target = best.Position;
                return true;
            }
            target = Home;
            return false;
        }

        void TouchPlayer()
        {
            if (Time.time < nextTouch)
                return;
            var session = GameSession.Instance;
            var player = session != null ? session.player : null;
            if (player == null || !player.isActiveAndEnabled || player.IsExtinguished)
                return;
            Vector2 d = (Vector2)player.transform.position - (Vector2)transform.position;
            if (d.magnitude > type.attackRange + 0.2f)
                return;
            nextTouch = Time.time + type.attackCooldown;
            var hit = HitData.FromEnemy(type.contactDamage, transform.position, d.normalized, 3f);
            if (player.Body.TakeHit(hit))
                Events.Play(SoundId.UnlitWhisper, transform.position);
        }

        sealed class Drift : State<EnemyBrain>
        {
            float nextWhisper;
            public override string Name => "Drift";

            public override void Tick(float dt)
            {
                var o = (UnlitBrain)Owner;
                bool drawn = o.PickTarget(out var target);
                o.Target = target;
                float speed = drawn ? o.type.chaseSpeed : o.type.wanderSpeed;
                if (Vector2.Distance(o.transform.position, target) > 0.4f)
                {
                    o.Motor.MoveTo(target, speed);
                    o.SetAnim(EnemyAnim.Move);
                }
                else
                {
                    o.Motor.Stop();
                    o.SetAnim(EnemyAnim.Idle);
                }
                o.TouchPlayer();
                if (drawn && Time.time >= nextWhisper)
                {
                    nextWhisper = Time.time + Random.Range(3f, 6f);
                    Events.Play(SoundId.UnlitWhisper, o.transform.position, 0.4f);
                }
            }
        }
    }
}
