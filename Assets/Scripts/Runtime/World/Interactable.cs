using UnityEngine;
using Wickfall.Player;

namespace Wickfall.World
{
    /// <summary>Anything the player can use with the interact key (braziers, bells, pedestals, relics, gates, shrines).</summary>
    public abstract class Interactable : MonoBehaviour
    {
        [Tooltip("Localization key of the on-screen prompt.")]
        public string promptKey = "prompt.interact";
        public Vector2 promptOffset = new Vector2(0f, 1.6f);

        public virtual bool CanInteract(PlayerController player) => isActiveAndEnabled;
        public abstract void Interact(PlayerController player);

        /// <summary>True when interacting should hold the player in the interact state (menus, rests).</summary>
        public virtual bool HoldsPlayer => false;
    }
}
