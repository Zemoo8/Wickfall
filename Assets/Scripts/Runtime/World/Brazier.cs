using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Wickfall.Lighting;
using Wickfall.Meta;
using Wickfall.Player;
using Wickfall.Presentation;

namespace Wickfall.World
{
    /// <summary>
    /// PDF §2.4 / §4 Brazier: "Point de contrôle : sauvegarde, remplit la cire, boutique d'éclats."
    /// "Remplit toute la cire, un usage par repos" (G08). Lit braziers are light sources creatures sense.
    /// </summary>
    public sealed class Brazier : Interactable
    {
        public string id = "brazier";
        public Transform respawnPoint;
        public SpriteRenderer sprite;
        public SpriteAnimator fire;
        public Light2D fireLight;
        public Sprite unlitSprite;
        public Sprite[] litFrames = new Sprite[0];
        public float lightRadius = 5.5f;
        public bool litAtStart;

        static readonly List<Brazier> all = new List<Brazier>();
        public static IReadOnlyList<Brazier> All => all;

        int sourceId;
        bool lit;
        float flicker;

        public bool IsLit => lit;
        public Vector2 RespawnPosition => respawnPoint != null ? (Vector2)respawnPoint.position : (Vector2)transform.position + Vector2.down;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        void OnEnable() => all.Add(this);

        void OnDisable()
        {
            all.Remove(this);
            LightSources.Remove(sourceId);
            sourceId = 0;
        }

        void Start()
        {
            bool known = GameSession.Instance != null && GameSession.Instance.HasFlag(FlagId);
            SetLit(litAtStart || known, false);
            promptKey = lit ? "prompt.rest" : "prompt.light";
        }

        string FlagId => "brazier/" + id;

        public override bool CanInteract(PlayerController player) =>
            base.CanInteract(player) && PresentationMode.IsEnabled(Feature.Braziers);

        public override bool HoldsPlayer => false;

        public override void Interact(PlayerController player)
        {
            var session = GameSession.Instance;
            if (!lit)
            {
                SetLit(true, true);
                session?.SetFlag(FlagId);
                promptKey = "prompt.rest";
            }
            bool refilled = session == null || session.CanRefillAt(id);
            session?.Rest(id, RespawnPosition);
            if (session == null)
                player.Wax.Refill();
            Events.Play(SoundId.BrazierRest, transform.position);
            Combat.Vfx.Play(Combat.VfxId.RelightBurst, (Vector2)transform.position + new Vector2(0f, 0.8f));
            BrazierMenu.Open?.Invoke(this, player, refilled);
        }

        void SetLit(bool value, bool announce)
        {
            lit = value;
            if (sprite != null && unlitSprite != null && !lit)
                sprite.sprite = unlitSprite;
            if (fire != null)
            {
                fire.gameObject.SetActive(lit);
                if (lit && litFrames.Length > 0)
                    fire.Play(litFrames, 8f, true);
            }
            if (fireLight != null)
                fireLight.enabled = lit;
            if (lit)
                sourceId = LightSources.Set(sourceId, transform.position, lightRadius);
            if (announce)
                Events.Play(SoundId.BrazierLight, transform.position);
        }

        void Update()
        {
            if (!lit || fireLight == null)
                return;
            flicker += Time.deltaTime * 6f;
            fireLight.intensity = 1.1f + (Mathf.PerlinNoise(flicker, 0.37f) - 0.5f) * 0.35f;
            fireLight.pointLightOuterRadius = lightRadius * (1f + (Mathf.PerlinNoise(0.11f, flicker) - 0.5f) * 0.06f);
        }
    }

    /// <summary>Hook for the brazier menu UI (rest, shop, loadout). Set by the HUD when it exists.</summary>
    public static class BrazierMenu
    {
        public static System.Action<Brazier, PlayerController, bool> Open;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Open = null;
    }
}
