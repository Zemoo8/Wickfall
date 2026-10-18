using UnityEngine;
using Wickfall.Combat;
using Wickfall.Presentation;
using Wickfall.World;

namespace Wickfall.Player
{
    /// <summary>
    /// PDF §4 "PlayerController et états : Machine à états : repos, marche, mode faible, attaque,
    /// esquive, blessé, éteint, interaction." Reads a <see cref="PlayerIntent"/> each frame (from
    /// the keyboard/gamepad or from a bot) and drives the systems on the same entity.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerController : MonoBehaviour
    {
        public WaxSystem Wax { get; private set; }
        public LightController Light { get; private set; }
        public DimController Dim { get; private set; }
        public FlameSwitcher Flames { get; private set; }
        public PlayerCombat Combat { get; private set; }
        public PlayerDodge Dodger { get; private set; }
        public PlayerStatus Status { get; private set; }
        public PlayerInteractor Interactor { get; private set; }
        public PlayerDamageable Body { get; private set; }

        Rigidbody2D rb;
        IPlayerInputSource source;
        readonly PlayerInputReader reader = new PlayerInputReader();
        PlayerIntent intent;

        StateMachine<PlayerController> fsm;
        IdleState idleState;
        WalkState walkState;
        DimState dimState;
        AttackState attackState;
        DodgeState dodgeState;
        HurtState hurtState;
        ExtinguishedState extinguishedState;
        InteractState interactState;

        float attackPressedAt = -10f;
        float artPressedAt = -10f;
        float dodgePressedAt = -10f;
        Vector2 bufferedAim;
        bool bufferedHasAim;
        Vector2 desiredVelocity;
        Vector2 impulse;
        int wheelSlot = -2;

        public Facing Facing { get; private set; } = Facing.Down;
        public Vector2 FacingVector { get; private set; } = Vector2.down;
        public Vector2 Velocity => rb != null ? rb.linearVelocity : Vector2.zero;
        public bool ControlsLocked { get; set; }
        public string StateName => fsm != null ? fsm.CurrentName : "-";
        public float TimeInState => fsm != null ? fsm.TimeInState : 0f;
        public bool IsDodgeInvulnerable => fsm != null && fsm.Is(dodgeState) && dodgeState.Invulnerable;
        public bool IsAttacking => fsm != null && fsm.Is(attackState);
        public bool AttackIsArt => attackState != null && attackState.IsArt;
        public float AttackProgress => fsm != null && fsm.Is(attackState) ? attackState.Progress : 0f;
        public bool IsDodging => fsm != null && fsm.Is(dodgeState);
        public float DodgeProgress => fsm != null && fsm.Is(dodgeState) ? dodgeState.Progress : 0f;
        public bool IsHurt => fsm != null && fsm.Is(hurtState);
        public bool IsExtinguished => fsm != null && fsm.Is(extinguishedState);
        public bool IsInteracting => fsm != null && fsm.Is(interactState);
        public bool IsMoving => desiredVelocity.sqrMagnitude > 0.01f && rb.linearVelocity.sqrMagnitude > 0.05f;
        public PlayerInputReader Reader => reader;
        public PlayerIntent Intent => intent;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            Wax = GetComponent<WaxSystem>();
            Light = GetComponent<LightController>();
            Dim = GetComponent<DimController>();
            Flames = GetComponent<FlameSwitcher>();
            Combat = GetComponent<PlayerCombat>();
            Dodger = GetComponent<PlayerDodge>();
            Status = GetComponent<PlayerStatus>();
            Interactor = GetComponent<PlayerInteractor>();
            Body = GetComponent<PlayerDamageable>();

            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            fsm = new StateMachine<PlayerController>(this);
            idleState = fsm.Add(new IdleState());
            walkState = fsm.Add(new WalkState());
            dimState = fsm.Add(new DimState());
            attackState = fsm.Add(new AttackState());
            dodgeState = fsm.Add(new DodgeState());
            hurtState = fsm.Add(new HurtState());
            extinguishedState = fsm.Add(new ExtinguishedState());
            interactState = fsm.Add(new InteractState());

            source = reader;
            intent.SelectSlot = -1;
        }

        void OnEnable() => Events.PlayerExtinguished.Register(OnExtinguished);
        void OnDisable() => Events.PlayerExtinguished.Unregister(OnExtinguished);

        void Start()
        {
            var controls = Db.Controls;
            reader.Mode = controls.defaultMovementMode;
            reader.TrimToggle = controls.defaultTrimToggle;
            reader.Deadzone = controls.stickDeadzone;
            fsm.Change(idleState);
        }

        /// <summary>Lets a bot (or a test) drive the player. Pass null to go back to the controls.</summary>
        public void SetInputSource(IPlayerInputSource value) => source = value ?? reader;

        public bool IsBotControlled => source != reader;

        void Update()
        {
            if (Time.timeScale <= 0f)
                return;

            intent.ClearPresses();
            if (!ControlsLocked)
                source.Read(ref intent);
            else
                intent.Move = Vector2.zero;

            float now = Time.time;
            if (intent.AttackPressed) { attackPressedAt = now; StoreAim(); }
            if (intent.ArtPressed) { artPressedAt = now; StoreAim(); }
            if (intent.DodgePressed) dodgePressedAt = now;

            bool canTrim = !IsExtinguished && !IsInteracting && !ControlsLocked;
            Dim.Request(canTrim && intent.TrimHeld);

            UpdateFlameWheel();
            fsm.Tick(Time.deltaTime);
        }

        void StoreAim()
        {
            bufferedHasAim = intent.HasAim;
            bufferedAim = intent.AimPoint;
        }

        void FixedUpdate()
        {
            fsm.FixedTick(Time.fixedDeltaTime);
            Vector2 target = desiredVelocity + Status.ExternalVelocity + impulse;
            var balance = Db.Balance;
            float rate = desiredVelocity.sqrMagnitude > 0.0001f ? balance.acceleration : balance.deceleration;
            Vector2 velocity = Vector2.MoveTowards(rb.linearVelocity, target, rate * Time.fixedDeltaTime);
            if (impulse.sqrMagnitude > 0f || IsDodging)
                velocity = target; // dodges and knockbacks are exact
            rb.linearVelocity = velocity;
            impulse = Vector2.MoveTowards(impulse, Vector2.zero, 40f * Time.fixedDeltaTime);
        }

        // ---- Flame switching and the wheel ------------------------------------------------------

        bool CanSwitchFlame => !IsExtinguished && !IsAttacking && !IsDodging && !IsInteracting && !ControlsLocked;

        void UpdateFlameWheel()
        {
            if (!CanSwitchFlame)
            {
                CloseWheel(false);
                return;
            }

            if (intent.SelectSlot >= 0)
                Flames.TrySelectSlot(intent.SelectSlot);
            else if (intent.CycleFlame != 0)
                Flames.TryCycle(intent.CycleFlame);

            if (intent.WheelHeld)
            {
                // While the wheel is held, the stick or WASD picks a slot instead of moving.
                int slot = wheelSlot < -1 ? -1 : wheelSlot;
                if (intent.Move.sqrMagnitude > 0.25f)
                    slot = WheelSlotFor(intent.Move, Flames.Slots);
                if (slot != wheelSlot)
                {
                    wheelSlot = slot;
                    Events.FlameWheel.Raise(slot);
                }
                intent.Move = Vector2.zero;
            }
            else
            {
                CloseWheel(true);
            }
        }

        void CloseWheel(bool apply)
        {
            if (wheelSlot < -1)
                return;
            int chosen = wheelSlot;
            wheelSlot = -2;
            Events.FlameWheel.Raise(-2);
            if (apply && chosen >= 0)
                Flames.TrySelectSlot(chosen);
        }

        /// <summary>Slot 0 at the top, going clockwise (matches the HUD wheel).</summary>
        public static int WheelSlotFor(Vector2 direction, int slots)
        {
            float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg; // 0 = up, clockwise
            if (angle < 0f) angle += 360f;
            float step = 360f / Mathf.Max(1, slots);
            return Mathf.FloorToInt((angle + step * 0.5f) / step) % Mathf.Max(1, slots);
        }

        // ---- Helpers used by the states ----------------------------------------------------------

        void Move(float speedMultiplier)
        {
            float speed = Db.Balance.moveSpeed * Status.MoveMultiplier * speedMultiplier;
            desiredVelocity = intent.Move * speed;
            if (intent.Move.sqrMagnitude > 0.01f)
                FaceTowards(intent.Move);
        }

        void Halt() => desiredVelocity = Vector2.zero;

        public void FaceTowards(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
                return;
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                Facing = direction.x > 0f ? Facing.Right : Facing.Left;
            else
                Facing = direction.y > 0f ? Facing.Up : Facing.Down;
            FacingVector = FacingToVector(Facing);
        }

        public static Vector2 FacingToVector(Facing facing)
        {
            switch (facing)
            {
                case Facing.Up: return Vector2.up;
                case Facing.Left: return Vector2.left;
                case Facing.Right: return Vector2.right;
                default: return Vector2.down;
            }
        }

        bool Buffered(float pressedAt) => Time.time - pressedAt <= Db.Controls.inputBufferSeconds;

        /// <summary>Starts a buffered action if one is waiting. Returns true if the state changed.</summary>
        bool TryStartAction()
        {
            if (Buffered(dodgePressedAt) && PresentationMode.IsEnabled(Feature.Dodge) && Dodger.TryConsume())
            {
                dodgePressedAt = -10f;
                fsm.Change(dodgeState);
                return true;
            }
            if (Buffered(artPressedAt))
            {
                artPressedAt = -10f;
                if (Combat.Begin(true))
                {
                    attackState.Setup(true, AimDirection());
                    fsm.Change(attackState);
                    return true;
                }
            }
            if (Buffered(attackPressedAt) && Combat.CanAttack)
            {
                attackPressedAt = -10f;
                if (Combat.Begin(false))
                {
                    attackState.Setup(false, AimDirection());
                    fsm.Change(attackState);
                    return true;
                }
            }
            if (intent.InteractPressed && Interactor.Current != null)
            {
                var target = Interactor.Current;
                Events.Play(SoundId.Interact, transform.position, 0.7f);
                if (target.HoldsPlayer)
                {
                    interactState.Target = target;
                    fsm.Change(interactState);
                }
                target.Interact(this);
                return target.HoldsPlayer;
            }
            return false;
        }

        Vector2 AimDirection()
        {
            if (bufferedHasAim)
            {
                Vector2 to = bufferedAim - ((Vector2)transform.position + new Vector2(0f, 0.45f));
                if (to.sqrMagnitude > 0.01f)
                {
                    FaceTowards(to);
                    return to.normalized;
                }
            }
            if (intent.Move.sqrMagnitude > 0.01f)
                FaceTowards(intent.Move);
            return FacingVector;
        }

        State<PlayerController> LocomotionState()
        {
            if (Dim.IsTrimmed) return dimState;
            return intent.Move.sqrMagnitude > 0.01f ? walkState : idleState;
        }

        // ---- Events from other systems -------------------------------------------------------------

        public void OnHurt(in HitData hit)
        {
            if (IsExtinguished)
                return;
            Vector2 away = hit.Direction.sqrMagnitude > 0.0001f ? hit.Direction : ((Vector2)transform.position - hit.Origin).normalized;
            impulse = away * Mathf.Max(hit.Knockback, Db.Balance.hurtKnockback * 0.5f);
            if (!IsDodging)
                fsm.Change(hurtState);
        }

        void OnExtinguished()
        {
            CloseWheel(false);
            Dim.SetTrimmed(false);
            fsm.Change(extinguishedState);
        }

        /// <summary>Back to life at a brazier (or wherever the session decides).</summary>
        public void Respawn(Vector2 position)
        {
            transform.position = position;
            rb.position = position;
            rb.linearVelocity = Vector2.zero;
            impulse = Vector2.zero;
            desiredVelocity = Vector2.zero;
            Status.ClearAll();
            Dodger.RefillAll();
            Facing = Facing.Down;
            FacingVector = Vector2.down;
            Physics2D.SyncTransforms();
            fsm.Change(idleState);
        }

        public void Teleport(Vector2 position)
        {
            transform.position = position;
            rb.position = position;
            rb.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
        }

        public void ReleaseFromInteraction()
        {
            if (IsInteracting)
                fsm.Change(LocomotionState());
        }

        public void AddImpulse(Vector2 velocity) => impulse += velocity;

        // ---- States ------------------------------------------------------------------------------

        sealed class IdleState : State<PlayerController>
        {
            public override string Name => "Idle";
            public override void Tick(float dt)
            {
                var p = Owner;
                p.Halt();
                if (p.TryStartAction()) return;
                if (p.Dim.IsTrimmed) { p.fsm.Change(p.dimState); return; }
                if (p.intent.Move.sqrMagnitude > 0.01f) p.fsm.Change(p.walkState);
            }
        }

        sealed class WalkState : State<PlayerController>
        {
            public override string Name => "Walk";
            public override void Tick(float dt)
            {
                var p = Owner;
                if (p.TryStartAction()) return;
                if (p.Dim.IsTrimmed) { p.fsm.Change(p.dimState); return; }
                if (p.intent.Move.sqrMagnitude <= 0.01f) { p.fsm.Change(p.idleState); return; }
                p.Move(1f);
            }
        }

        /// <summary>"Mode faible": walking or standing with the wick trimmed.</summary>
        sealed class DimState : State<PlayerController>
        {
            public override string Name => "Dim";
            public override void Tick(float dt)
            {
                var p = Owner;
                if (p.TryStartAction()) return;
                if (!p.Dim.IsTrimmed) { p.fsm.Change(p.LocomotionState()); return; }
                if (p.intent.Move.sqrMagnitude > 0.01f) p.Move(1f);
                else p.Halt();
            }
        }

        sealed class AttackState : State<PlayerController>
        {
            public bool IsArt;
            Vector2 direction;
            float duration;
            float hitAt;
            bool hitDone;
            public bool Interrupted;

            public override string Name => IsArt ? "Art" : "Attack";
            public float Progress => duration > 0f ? Mathf.Clamp01(Owner.fsm.TimeInState / duration) : 1f;

            public void Setup(bool art, Vector2 dir)
            {
                IsArt = art;
                direction = dir;
            }

            public override void Enter()
            {
                var p = Owner;
                duration = p.Combat.AttackDuration(IsArt);
                hitAt = p.Combat.HitMoment(IsArt);
                hitDone = false;
                p.Halt();
            }

            public override void Tick(float dt)
            {
                var p = Owner;
                float t = p.fsm.TimeInState;
                if (!hitDone && t >= hitAt)
                {
                    hitDone = true;
                    p.Combat.Execute(IsArt, direction);
                    p.impulse += direction * (IsArt ? 2f : 1.6f); // a small step into the swing
                }
                if (t >= duration)
                {
                    // Chain straight into a buffered action, otherwise back to moving.
                    if (!p.TryStartAction())
                        p.fsm.Change(p.LocomotionState());
                }
            }
        }

        sealed class DodgeState : State<PlayerController>
        {
            Vector2 direction;
            public bool Invulnerable => Owner.fsm.TimeInState < Db.Balance.dodgeInvulnerableSeconds;
            public float Progress => Mathf.Clamp01(Owner.fsm.TimeInState / Mathf.Max(0.01f, Db.Balance.dodgeSeconds));
            public override string Name => "Dodge";

            public override void Enter()
            {
                var p = Owner;
                direction = p.intent.Move.sqrMagnitude > 0.01f ? p.intent.Move.normalized : p.FacingVector;
                p.FaceTowards(direction);
                var b = Db.Balance;
                p.desiredVelocity = direction * (b.dodgeDistance / Mathf.Max(0.01f, b.dodgeSeconds));
                Vector2 position = p.transform.position;
                Events.PlayerActed.Raise(new PlayerActionInfo
                {
                    Kind = PlayerActionKind.Dodge, Direction = direction, Flame = p.Flames.Current, Position = position, Time = Time.time,
                });
                Events.Play(SoundId.Dodge, position);
                Vfx.Play(VfxId.DustPuff, position, -direction);
            }

            public override void Tick(float dt)
            {
                var p = Owner;
                if (p.fsm.TimeInState >= Db.Balance.dodgeSeconds)
                {
                    p.desiredVelocity = direction * Db.Balance.moveSpeed * 0.5f;
                    if (!p.TryStartAction())
                        p.fsm.Change(p.LocomotionState());
                }
            }

            public override void Exit() => Owner.Halt();
        }

        sealed class HurtState : State<PlayerController>
        {
            public override string Name => "Hurt";
            public override void Enter() => Owner.Halt();
            public override void Tick(float dt)
            {
                var p = Owner;
                if (p.fsm.TimeInState >= Db.Balance.hurtSeconds)
                    p.fsm.Change(p.LocomotionState());
            }
        }

        /// <summary>"Éteint": the flame is out. The session respawns the player at the last brazier.</summary>
        sealed class ExtinguishedState : State<PlayerController>
        {
            public override string Name => "Extinguished";
            public override void Enter()
            {
                var p = Owner;
                p.Halt();
                p.impulse = Vector2.zero;
                Vector2 position = p.transform.position;
                Events.Play(SoundId.Extinguish, position);
                Vfx.Play(VfxId.ExtinguishBurst, position + new Vector2(0f, 1.8f));
            }
        }

        sealed class InteractState : State<PlayerController>
        {
            public Interactable Target;
            public override string Name => "Interact";
            public override void Enter()
            {
                Owner.Halt();
                Owner.Interactor.Hide();
            }

            public override void Exit()
            {
                Target = null;
                Owner.Interactor.Refresh();
            }
        }
    }
}
