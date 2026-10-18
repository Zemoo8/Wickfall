using UnityEngine;
using UnityEngine.Rendering.Universal;
using Wickfall.Combat;
using Wickfall.Meta;
using Wickfall.Player;

namespace Wickfall.World
{
    /// <summary>
    /// PDF §2.4: "Pactes du Roi Creux (optionnels) : puissance contre un coût permanent (ex. +30 cire
    /// maximale, perte x1,5)". A shrine where the Roi Creux's voice offers one pact (G11). Accepting is
    /// permanent; refusing costs nothing.
    /// </summary>
    public sealed class PactShrine : Interactable
    {
        public int pactIndex;
        public SpriteRenderer sprite;
        public Light2D shade;

        bool Taken => GameSession.Instance != null && (GameSession.Instance.Save.pactsMask & (1 << pactIndex)) != 0;

        void Start()
        {
            promptKey = "prompt.pact";
            Show();
        }

        public override bool CanInteract(PlayerController player) => base.CanInteract(player) && !Taken && !Choice.IsOpen;

        public override void Interact(PlayerController player)
        {
            var pact = Db.Data.pacts[pactIndex];
            Events.Play(SoundId.KingVoid, transform.position, 0.5f);
            Choice.Ask(new ChoiceRequest
            {
                TitleKey = pact.nameKey,
                BodyText = UI.Loc.Get("pact.voice") + "\n" + UI.Loc.Get(pact.descriptionKey),
                OptionKeys = new[] { "pact.accept", "pact.refuse" },
                OnChosen = option =>
                {
                    if (option != 0)
                        return;
                    GameSession.Instance?.TakePact(pactIndex);
                    Vfx.Play(VfxId.VoidBurst, (Vector2)transform.position + new Vector2(0f, 1f));
                    Events.Shake(0.1f, 0.4f);
                    Show();
                },
            });
        }

        void Show()
        {
            if (shade != null)
                shade.intensity = Taken ? 0.02f : 0.12f;
            if (sprite != null)
                sprite.color = Taken ? new Color(0.5f, 0.5f, 0.5f, 1f) : Color.white;
        }
    }
}
