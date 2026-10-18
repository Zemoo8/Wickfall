using UnityEngine;
using UnityEngine.Rendering.Universal;
using Wickfall.Meta;
using Wickfall.Player;

namespace Wickfall.World
{
    /// <summary>
    /// PDF §4 WaxPool: "bassins de recharge dans le monde". Standing in a pool and using it drinks its
    /// wax into the flame (G32: a pool holds 35 wax and fills again when you rest at a brazier).
    /// The Roi Creux's first phase darkens the arena's pools (G13).
    /// </summary>
    public sealed class WaxPool : Interactable
    {
        public string id = "pool";
        public float capacity = 35f;
        public float drinkPerSecond = 40f;
        public SpriteRenderer sprite;
        public Sprite fullSprite;
        public Sprite emptySprite;
        public Light2D glow;
        public bool bossArena;

        float remaining;
        bool drinking;
        PlayerController drinker;
        bool disabledByBoss;

        public float Remaining => remaining;

        void OnEnable()
        {
            Events.BrazierRested.Register(OnRest);
            Events.BossPhaseChanged.Register(OnBossPhase);
            Events.PlayerRespawned.Register(OnRest0);
        }

        void OnDisable()
        {
            Events.BrazierRested.Unregister(OnRest);
            Events.BossPhaseChanged.Unregister(OnBossPhase);
            Events.PlayerRespawned.Unregister(OnRest0);
        }

        void Start()
        {
            bool drained = GameSession.Instance != null && GameSession.Instance.HasFlag("pool/" + id);
            remaining = drained ? 0f : capacity;
            promptKey = "prompt.drink";
            Refresh();
        }

        void OnRest(int _) => Fill();
        void OnRest0() => Fill();

        void Fill()
        {
            remaining = capacity;
            Refresh();
        }

        void OnBossPhase(int phase)
        {
            if (!bossArena)
                return;
            disabledByBoss = ProgressRules.WaxPoolsDisabled(phase, Db.Boss);
            Refresh();
        }

        public override bool CanInteract(PlayerController player) =>
            base.CanInteract(player) && remaining > 0.5f && !disabledByBoss && player.Wax.Current < player.Wax.Max - 0.5f;

        public override void Interact(PlayerController player)
        {
            drinker = player;
            drinking = true;
            Events.Play(SoundId.WaxPoolDrink, transform.position);
        }

        void Update()
        {
            if (!drinking || drinker == null)
                return;
            if (disabledByBoss || remaining <= 0f || Vector2.Distance(drinker.transform.position, transform.position) > 1.6f
                || drinker.Wax.Current >= drinker.Wax.Max - 0.01f)
            {
                drinking = false;
                Refresh();
                return;
            }
            float amount = Mathf.Min(remaining, drinkPerSecond * Time.deltaTime);
            remaining -= drinker.Wax.Gain(amount);
            if (remaining <= 0.01f)
            {
                remaining = 0f;
                GameSession.Instance?.SetFlag("pool/" + id);
            }
            Refresh();
        }

        void Refresh()
        {
            bool full = remaining > 0.5f && !disabledByBoss;
            if (sprite != null)
                sprite.sprite = full ? fullSprite : emptySprite;
            if (glow != null)
            {
                glow.enabled = full;
                glow.intensity = 0.4f + 0.6f * (remaining / Mathf.Max(1f, capacity));
            }
        }
    }
}
