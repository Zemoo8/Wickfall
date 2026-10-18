using UnityEngine;
using UnityEngine.Rendering.Universal;
using Wickfall.Meta;
using Wickfall.Player;
using Wickfall.Presentation;

namespace Wickfall.World
{
    /// <summary>PDF §2.4: one Ember Relic per region. Glows, hovers, and is gone once taken.</summary>
    public sealed class RelicPickup : Interactable
    {
        [Range(0, 5)] public int relicIndex;
        public SpriteRenderer sprite;
        public Light2D glow;

        float seed;

        void Start()
        {
            promptKey = "prompt.relic";
            seed = Random.value * 10f;
            var session = GameSession.Instance;
            if ((session != null && session.Save.HasRelic(relicIndex)) || !PresentationMode.IsEnabled(Feature.Relics))
                gameObject.SetActive(false);
            var relic = Db.Data.relics[relicIndex];
            if (glow != null && relic != null)
                glow.color = relic.glow;
        }

        public override void Interact(PlayerController player)
        {
            GameSession.Instance?.CollectRelic(relicIndex);
            Combat.Vfx.Play(Combat.VfxId.RelightBurst, transform.position, default, Db.Data.relics[relicIndex].glow, 1.5f);
            Events.Shake(0.1f, 0.3f);
            UI.ItemCard.Show?.Invoke(Db.Data.relics[relicIndex].nameKey, Db.Data.relics[relicIndex].descriptionKey, UI.ItemCard.Kind.Relic);
            gameObject.SetActive(false);
        }

        void Update()
        {
            if (sprite != null)
                sprite.transform.localPosition = new Vector3(0f, 0.6f + Mathf.Round(Mathf.Sin((Time.time + seed) * 2f) * 2f) / 16f, 0f);
            if (glow != null)
                glow.intensity = 0.9f + Mathf.Sin((Time.time + seed) * 3f) * 0.15f;
        }
    }
}
