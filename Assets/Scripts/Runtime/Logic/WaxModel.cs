using System;
using UnityEngine;

namespace Wickfall
{
    /// <summary>
    /// The wax of PDF §2.1, as plain C# so it can be proven by EditMode tests. WaxSystem (the only
    /// owner of wax, PDF §4) wraps it. Wax at exactly 0 is death: "la flamme s'éteint (cire à 0)".
    /// </summary>
    public sealed class WaxModel
    {
        float current;
        float baseMax;
        float cap = float.PositiveInfinity;

        public float Current => current;
        /// <summary>Maximum after relics, pacts and the boss's phase-3 cap.</summary>
        public float Max => Mathf.Min(baseMax, cap);
        public float BaseMax => baseMax;
        public float Cap => cap;
        public bool IsExtinguished { get; private set; }
        public float Normalized => Max > 0f ? current / Max : 0f;

        /// <summary>Wax lost per second before multipliers (PDF: 0.5).</summary>
        public float PassiveLossPerSecond { get; set; }

        /// <summary>
        /// Product of every multiplier on passive loss: the region (Creux Profond x1.5), the lit
        /// flame's "Coût en cire" (G01), trimming (x0.25) and pacts.
        /// </summary>
        public float LossMultiplier { get; set; } = 1f;

        /// <summary>Raised on every change (the WaxSystem forwards it to the WaxChanged channel).</summary>
        public Action<WaxChange> Changed;
        /// <summary>Raised once when wax reaches 0.</summary>
        public Action Extinguished;

        public WaxModel(float max, float passiveLossPerSecond)
        {
            baseMax = max;
            current = max;
            PassiveLossPerSecond = passiveLossPerSecond;
        }

        public float CurrentLossPerSecond => PassiveLossPerSecond * LossMultiplier;

        /// <summary>PDF: "rester immobile, c'est perdre" - wax always drains, moving or not.</summary>
        public void Tick(float deltaTime)
        {
            if (IsExtinguished || deltaTime <= 0f)
                return;
            Remove(CurrentLossPerSecond * deltaTime, WaxChangeReason.Passive);
        }

        /// <summary>Removes wax from a hit. Returns the wax actually removed.</summary>
        public float Damage(float amount) => Remove(amount, WaxChangeReason.Damage);

        /// <summary>Pays a cost (flame switch, art). Returns false and changes nothing if wax is too low to stay lit.</summary>
        public bool TrySpend(float amount)
        {
            if (IsExtinguished || amount <= 0f)
                return !IsExtinguished;
            if (current - amount <= 0f)
                return false;
            Remove(amount, WaxChangeReason.Cost);
            return true;
        }

        public float Gain(float amount)
        {
            if (IsExtinguished || amount <= 0f)
                return 0f;
            float before = current;
            current = Mathf.Min(Max, current + amount);
            float delta = current - before;
            if (delta != 0f)
                Raise(delta, WaxChangeReason.Gain);
            return delta;
        }

        /// <summary>Brazier rest or relight: full wax (PDF §2.4 "remplit toute la cire").</summary>
        public void Refill(WaxChangeReason reason = WaxChangeReason.Refill)
        {
            float before = current;
            IsExtinguished = false;
            current = Max;
            Raise(current - before, reason);
        }

        /// <summary>Relights an extinguished flame with some wax (respawn, Spare Wick relic).</summary>
        public void Revive(float amount)
        {
            float before = current;
            IsExtinguished = false;
            current = Mathf.Clamp(amount, 0.01f, Max);
            Raise(current - before, WaxChangeReason.Revive);
        }

        /// <summary>Relics and pacts change the base maximum; current wax keeps its value but never exceeds the new max.</summary>
        public void SetBaseMax(float value)
        {
            baseMax = Mathf.Max(1f, value);
            ClampToMax(WaxChangeReason.MaxChanged);
        }

        /// <summary>The Roi Creux's phase 3 caps max wax at 40 (PDF §4). Pass +Infinity to lift it.</summary>
        public void SetCap(float value)
        {
            cap = value;
            ClampToMax(WaxChangeReason.MaxChanged);
        }

        public void SetCurrent(float value, WaxChangeReason reason = WaxChangeReason.Load)
        {
            float before = current;
            current = Mathf.Clamp(value, 0f, Max);
            IsExtinguished = current <= 0f;
            Raise(current - before, reason);
        }

        void ClampToMax(WaxChangeReason reason)
        {
            float before = current;
            if (current > Max)
                current = Max;
            Raise(current - before, reason);
        }

        float Remove(float amount, WaxChangeReason reason)
        {
            if (IsExtinguished || amount <= 0f)
                return 0f;
            float before = current;
            current = Mathf.Max(0f, current - amount);
            float removed = before - current;
            Raise(-removed, reason);
            if (current <= 0f)
            {
                current = 0f;
                IsExtinguished = true;
                Raise(0f, WaxChangeReason.Death);
                Extinguished?.Invoke();
            }
            return removed;
        }

        void Raise(float delta, WaxChangeReason reason) =>
            Changed?.Invoke(new WaxChange(current, Max, delta, reason));
    }
}
