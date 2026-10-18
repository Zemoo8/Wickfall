using UnityEngine;
using UnityEngine.Rendering.Universal;
using Wickfall.Meta;

namespace Wickfall.World
{
    /// <summary>
    /// The end of a light puzzle (prompt §8 LightPuzzle): a crystal socket set in the wall. Held in a
    /// beam for a moment it wakes and opens its gate for good.
    /// </summary>
    public sealed class BeamSocket : MonoBehaviour, IBeamReceiver
    {
        public string gateId = "";
        public string id = "";
        public SpriteRenderer sprite;
        public Sprite dark;
        public Sprite lit;
        public Light2D glow;
        public float holdSeconds = 0.4f;

        float litUntil;
        float heldFor;
        public bool Lit => Time.time < litUntil;
        public bool Solved { get; private set; }

        void Start()
        {
            Solved = !string.IsNullOrEmpty(id) && GameSession.Instance != null && GameSession.Instance.HasFlag("socket/" + id);
            Show();
        }

        public bool Receive(BeamKind kind, Vector2 point, Vector2 direction, Vector2 normal, out Vector2 outOrigin, out Vector2 outDirection)
        {
            if (kind == BeamKind.Light)
                litUntil = Time.time + 0.15f;
            outOrigin = point;
            outDirection = direction;
            return false;
        }

        void Update()
        {
            heldFor = Lit ? heldFor + Time.deltaTime : 0f;
            if (!Solved && heldFor >= holdSeconds)
            {
                Solved = true;
                if (!string.IsNullOrEmpty(id))
                    GameSession.Instance?.SetFlag("socket/" + id);
                Events.Play(SoundId.PanelLit, transform.position);
                if (!string.IsNullOrEmpty(gateId))
                    Gate.Find(gateId)?.Open();
            }
            Show();
        }

        void Show()
        {
            bool on = Solved || Lit;
            if (sprite != null)
                sprite.sprite = on ? (lit != null ? lit : dark) : dark;
            if (glow != null)
            {
                glow.enabled = on;
                glow.intensity = Lit ? 1.1f : 0.5f;
            }
        }
    }
}
