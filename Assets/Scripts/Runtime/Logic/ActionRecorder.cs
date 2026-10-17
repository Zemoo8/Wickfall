namespace Wickfall
{
    /// <summary>
    /// Remembers the player's last three actions for the Reflections (PDF §2.2: "copient vos 3
    /// dernières actions"). A fixed ring buffer: recording never allocates.
    /// </summary>
    public sealed class ActionRecorder
    {
        public const int Capacity = 3;

        readonly PlayerActionInfo[] actions = new PlayerActionInfo[Capacity];
        int next;
        int count;

        public int Count => count;

        public void Record(in PlayerActionInfo action)
        {
            if (action.Kind == PlayerActionKind.None)
                return;
            actions[next] = action;
            next = (next + 1) % Capacity;
            if (count < Capacity)
                count++;
        }

        /// <summary>Oldest-first: index 0 is the oldest of the remembered actions.</summary>
        public PlayerActionInfo Get(int index)
        {
            int start = (next - count + Capacity) % Capacity;
            return actions[(start + index) % Capacity];
        }

        public void Clear()
        {
            next = 0;
            count = 0;
        }
    }
}
