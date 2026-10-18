using System.Collections;
using UnityEngine;
using Wickfall.Meta;
using Wickfall.Player;

namespace Wickfall.World
{
    /// <summary>
    /// PDF §2.2 Bois-Mèche: "vignes à grimper" — wick-vines that climb a wall. They need the second
    /// Ember Relic, "accroche au mur" (G04). Using a vine carries the player to its other end.
    /// </summary>
    public sealed class Vine : Interactable
    {
        public Vector2 bottom;   // world positions of the two ends (floor cells beyond the strip)
        public Vector2 top;
        public float climbSpeed = 3.2f;

        bool climbing;

        void Start() => promptKey = "prompt.climb";

        public override bool CanInteract(PlayerController player) => base.CanInteract(player) && !climbing;

        public override void Interact(PlayerController player)
        {
            var session = GameSession.Instance;
            if (session == null || !session.Modifiers.WallGrip)
            {
                Events.PlayUi(SoundId.UiError);
                UI.ItemCard.Show?.Invoke(null, "vine.locked", UI.ItemCard.Kind.Inscription);
                return;
            }
            StartCoroutine(Climb(player));
        }

        IEnumerator Climb(PlayerController player)
        {
            climbing = true;
            Vector2 position = player.transform.position;
            bool up = (position - bottom).sqrMagnitude < (position - top).sqrMagnitude;
            Vector2 from = up ? bottom : top;
            Vector2 to = up ? top : bottom;
            player.ControlsLocked = true;
            var rb = player.GetComponent<Rigidbody2D>();
            rb.simulated = false;
            player.FaceTowards(to - from);
            Events.Play(SoundId.Interact, position);
            float length = Vector2.Distance(from, to);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * climbSpeed / Mathf.Max(0.1f, length);
                Vector2 p = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                player.transform.position = p;
                yield return null;
            }
            rb.simulated = true;
            player.Teleport(to);
            player.ControlsLocked = false;
            climbing = false;
        }
    }
}
