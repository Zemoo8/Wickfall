using System;
using UnityEngine;

namespace Wickfall
{
    /// <summary>
    /// PDF §4 "EventChannels": systems send messages through these assets instead of holding
    /// references to each other. A channel remembers the last value it carried, so a listener that
    /// starts late (the HUD after a scene load) can read the current state at once.
    /// </summary>
    public abstract class EventChannelBase : ScriptableObject
    {
        [TextArea(1, 3)] public string description;

        /// <summary>How many listeners are registered (shown by the dev overlay).</summary>
        public abstract int ListenerCount { get; }

        /// <summary>Forgets the remembered value (new game, tests).</summary>
        public abstract void ResetState();
    }

    public abstract class EventChannel<T> : EventChannelBase
    {
        event Action<T> raised;
        T last;
        bool hasLast;
        int listeners;

        public T Last => last;
        public bool HasLast => hasLast;
        public override int ListenerCount => listeners;

        public void Register(Action<T> listener)
        {
            raised += listener;
            listeners++;
        }

        public void Unregister(Action<T> listener)
        {
            raised -= listener;
            listeners = Mathf.Max(0, listeners - 1);
        }

        public void Raise(T value)
        {
            last = value;
            hasLast = true;
            raised?.Invoke(value);
        }

        public override void ResetState()
        {
            last = default;
            hasLast = false;
        }

        void OnDisable() => ResetState();
    }

    /// <summary>A channel without a payload.</summary>
    public abstract class SignalChannel : EventChannelBase
    {
        event Action raised;
        int listeners;
        int count;

        public override int ListenerCount => listeners;
        public int RaiseCount => count;

        public void Register(Action listener)
        {
            raised += listener;
            listeners++;
        }

        public void Unregister(Action listener)
        {
            raised -= listener;
            listeners = Mathf.Max(0, listeners - 1);
        }

        public void Raise()
        {
            count++;
            raised?.Invoke();
        }

        public override void ResetState() => count = 0;

        void OnDisable() => ResetState();
    }
}
