using UnityEngine;
using Wickfall.Meta;

namespace Wickfall.Player
{
    /// <summary>
    /// PDF §4 WaxSystem: "Seul propriétaire de la cire. Gère perte passive, dégâts, gains, mort.
    /// Tout le reste lit ou écoute cette valeur." Every change goes out on the WaxChanged channel;
    /// wax at 0 raises PlayerExtinguished. The loss multipliers arrive through channels too
    /// (region entered, flame switched, trim changed, boss phase).
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class WaxSystem : MonoBehaviour
    {
        WaxModel model;
        float regionMultiplier = 1f;
        float flameCostMultiplier = 1f;
        float trimMultiplier = 1f;
        float pactMultiplier = 1f;
        int bossPhase;

        public bool Ticking { get; set; } = true;
        public bool GodMode { get; set; }

        public float Current => Model.Current;
        public float Max => Model.Max;
        public float Normalized => Model.Normalized;
        public bool IsExtinguished => Model.IsExtinguished;
        public float LossPerSecond => Model.CurrentLossPerSecond;
        public WaxModel Model
        {
            get
            {
                if (model == null)
                    CreateModel();
                return model;
            }
        }

        void CreateModel()
        {
            model = new WaxModel(Db.Wax.maxWax, Db.Wax.passiveLossPerSecond);
            model.Changed = OnModelChanged;
            model.Extinguished = OnModelExtinguished;
        }

        void Awake()
        {
            if (model == null)
                CreateModel();
        }

        void OnEnable()
        {
            Events.RegionEntered.Register(OnRegionEntered);
            Events.FlameSwitched.Register(OnFlameSwitched);
            Events.TrimChanged.Register(OnTrimChanged);
            Events.BossPhaseChanged.Register(OnBossPhase);
        }

        void OnDisable()
        {
            Events.RegionEntered.Unregister(OnRegionEntered);
            Events.FlameSwitched.Unregister(OnFlameSwitched);
            Events.TrimChanged.Unregister(OnTrimChanged);
            Events.BossPhaseChanged.Unregister(OnBossPhase);
        }

        void Start() => Publish(0f, WaxChangeReason.Load);

        void Update()
        {
            Model.LossMultiplier = regionMultiplier * flameCostMultiplier * trimMultiplier * pactMultiplier;
            if (Ticking && !GodMode)
                Model.Tick(Time.deltaTime);
        }

        void OnRegionEntered(RegionId region)
        {
            var data = Db.Region(region);
            regionMultiplier = data != null ? data.waxLossMultiplier : 1f; // Creux Profond: x1.5
        }

        // G01: the lit flame's "Coût en cire" multiplies passive loss.
        void OnFlameSwitched(FlameSwitch change) => flameCostMultiplier = Db.Flame(change.To).waxCostMultiplier;

        // PDF: trimming the wick cuts wax loss by 75 %.
        void OnTrimChanged(bool trimmed) => trimMultiplier = trimmed ? Db.Dim.waxLossMultiplier : 1f;

        void OnBossPhase(int phase)
        {
            bossPhase = phase;
            Model.SetCap(ProgressRules.MaxWaxCap(phase, Db.Boss)); // PDF: phase 3 caps max wax at 40
        }

        /// <summary>Relics and pacts (max wax, loss multiplier).</summary>
        public void ApplyModifiers(in PlayerModifiers modifiers)
        {
            pactMultiplier = modifiers.WaxLossMultiplier;
            Model.SetBaseMax(modifiers.MaxWax);
        }

        public void SetFlame(FlameId flame) => flameCostMultiplier = Db.Flame(flame).waxCostMultiplier;

        public float Damage(float amount)
        {
            if (GodMode)
                return 0f;
            return Model.Damage(amount);
        }

        public float Gain(float amount) => Model.Gain(amount);
        public bool TrySpend(float amount) => GodMode || Model.TrySpend(amount);
        public void Refill() => Model.Refill();
        public void Revive(float amount) => Model.Revive(amount);
        public void SetCurrent(float value) => Model.SetCurrent(value);
        public int BossPhase => bossPhase;

        void OnModelChanged(WaxChange change) => Events.WaxChanged.Raise(change);

        void OnModelExtinguished() => Events.PlayerExtinguished.Raise();

        void Publish(float delta, WaxChangeReason reason) =>
            Events.WaxChanged.Raise(new WaxChange(Model.Current, Model.Max, delta, reason));
    }
}
