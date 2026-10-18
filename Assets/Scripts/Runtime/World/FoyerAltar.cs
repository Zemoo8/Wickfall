using UnityEngine;
using UnityEngine.Rendering.Universal;
using Wickfall.Combat;
using Wickfall.Meta;
using Wickfall.Player;

namespace Wickfall.World
{
    /// <summary>
    /// The Grand Foyer (PDF §2.2: "Foyer en lumière négative"). A vast cold hearth that drinks the light
    /// around it. Once the Roi Creux's fate is decided, the Wickbearer chooses (PDF §1, G12):
    /// A) relight the Foyer and die, B) keep the flame and stay alone, C) pass the wick on — offered only
    /// with the 6 relics and the King spared.
    /// </summary>
    public sealed class FoyerAltar : Interactable
    {
        public Light2D negative;
        public Light2D relit;
        public SpriteRenderer sprite;

        void Start()
        {
            promptKey = "prompt.foyer";
            if (relit != null)
                relit.enabled = false;
        }

        void Update()
        {
            if (negative != null && negative.enabled)
                negative.intensity = 0.55f + 0.08f * Mathf.Sin(Time.time * 0.8f);
        }

        public override bool CanInteract(PlayerController player)
        {
            var session = GameSession.Instance;
            return base.CanInteract(player) && session != null && session.Save.kingDecided && !Choice.IsOpen;
        }

        public override void Interact(PlayerController player)
        {
            var save = GameSession.Instance.Save;
            bool secret = ProgressRules.CanPassTheWick(save);
            Choice.Ask(new ChoiceRequest
            {
                TitleKey = "foyer.title",
                BodyKey = secret ? "foyer.body.c" : "foyer.body",
                OptionKeys = new[] { "foyer.relight", "foyer.keep", "foyer.pass" },
                Enabled = new[] { true, true, secret },
                OnChosen = option => Choose((EndingId)option),
            });
        }

        void Choose(EndingId ending)
        {
            if (ending == EndingId.A)
            {
                if (negative != null) negative.enabled = false;
                if (relit != null) relit.enabled = true;
                Vfx.Play(VfxId.RelightBurst, (Vector2)transform.position + new Vector2(0f, 2f));
                Events.Play(SoundId.BrazierLight, transform.position);
                Events.Shake(0.2f, 0.8f);
            }
            GameSession.Instance?.ReachEnding(ending);
        }
    }
}
