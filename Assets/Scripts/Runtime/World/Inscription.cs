using UnityEngine;
using Wickfall.Player;

namespace Wickfall.World
{
    /// <summary>A stone with a short line of text (G16: only short inscriptions until the story is approved).</summary>
    public sealed class Inscription : Interactable
    {
        public string textKey = "inscription.r1.0";

        void Start() => promptKey = "prompt.read";

        public override void Interact(PlayerController player)
        {
            Events.PlayUi(SoundId.UiOpen);
            UI.ItemCard.Show?.Invoke(null, textKey, UI.ItemCard.Kind.Inscription);
        }
    }
}
