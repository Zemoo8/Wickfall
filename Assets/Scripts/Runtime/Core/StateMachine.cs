namespace Wickfall
{
    /// <summary>
    /// One state of a <see cref="StateMachine{TOwner}"/>. States are created once when their owner
    /// starts, so changing state never allocates (PDF §4: "chaque entité a une machine à états").
    /// </summary>
    public abstract class State<TOwner> where TOwner : class
    {
        protected TOwner Owner { get; private set; }

        /// <summary>Shown by the dev overlay. Return a constant string.</summary>
        public abstract string Name { get; }

        internal void Bind(TOwner owner) => Owner = owner;

        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void Tick(float deltaTime) { }
        public virtual void FixedTick(float deltaTime) { }
    }

    /// <summary>A small, allocation-free finite state machine used by the player, every enemy, the boss and the game flow.</summary>
    public sealed class StateMachine<TOwner> where TOwner : class
    {
        readonly TOwner owner;
        bool changing;
        State<TOwner> pending;

        public State<TOwner> Current { get; private set; }
        public State<TOwner> Previous { get; private set; }
        public float TimeInState { get; private set; }
        public int Changes { get; private set; }
        public string CurrentName => Current != null ? Current.Name : "-";

        public StateMachine(TOwner owner) => this.owner = owner;

        /// <summary>Registers a state with this machine's owner. Call once per state at startup.</summary>
        public TState Add<TState>(TState state) where TState : State<TOwner>
        {
            state.Bind(owner);
            return state;
        }

        public bool Is(State<TOwner> state) => Current == state;

        /// <summary>
        /// Leaves the current state and enters <paramref name="next"/>. A change requested from
        /// inside Enter or Exit is queued and applied right after, so states never nest.
        /// </summary>
        public void Change(State<TOwner> next)
        {
            if (changing)
            {
                pending = next;
                return;
            }

            while (next != null && next != Current)
            {
                changing = true;
                Current?.Exit();
                Previous = Current;
                Current = next;
                TimeInState = 0f;
                Changes++;
                Current.Enter();
                changing = false;

                next = pending;
                pending = null;
            }
        }

        public void Tick(float deltaTime)
        {
            TimeInState += deltaTime;
            Current?.Tick(deltaTime);
        }

        public void FixedTick(float deltaTime) => Current?.FixedTick(deltaTime);
    }
}
