using UnityEngine;

namespace Wickfall.Player
{
    /// <summary>
    /// The dodge (PDF "esquive", drawn as a dash with squash-and-stretch and a wax trail). Charges
    /// recharge one at a time (G05); the first relic adds a charge ("esquive en plus").
    /// </summary>
    public sealed class PlayerDodge : MonoBehaviour
    {
        public int MaxCharges { get; private set; } = 1;
        public int Charges { get; private set; } = 1;
        float recharge;

        public float RechargeProgress => Charges >= MaxCharges ? 1f : recharge / Mathf.Max(0.01f, Db.Balance.dodgeRechargeSeconds);

        void Start() => Publish();

        public void SetMaxCharges(int value)
        {
            MaxCharges = Mathf.Max(1, value);
            Charges = MaxCharges;
            recharge = 0f;
            Publish();
        }

        public bool TryConsume()
        {
            if (Charges <= 0)
                return false;
            Charges--;
            Publish();
            return true;
        }

        public void RefillAll()
        {
            Charges = MaxCharges;
            recharge = 0f;
            Publish();
        }

        void Update()
        {
            if (Charges >= MaxCharges)
                return;
            recharge += Time.deltaTime;
            if (recharge >= Db.Balance.dodgeRechargeSeconds)
            {
                recharge = 0f;
                Charges++;
                Publish();
            }
        }

        void Publish() => Events.DodgeChanged.Raise(new DodgeState
        {
            Charges = Charges, MaxCharges = MaxCharges, RechargeProgress = RechargeProgress,
        });
    }
}
