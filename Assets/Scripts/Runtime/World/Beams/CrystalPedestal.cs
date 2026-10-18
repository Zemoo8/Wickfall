using UnityEngine;
using Wickfall.Player;

namespace Wickfall.World
{
    /// <summary>
    /// A32 crystal pedestal (prompt §8 LightPuzzle: "beams aimed with pedestals"). Interact to turn the
    /// crystal 45°; any beam that reaches it leaves along the crystal's facing.
    /// </summary>
    public sealed class CrystalPedestal : Interactable, IBeamReceiver
    {
        [Range(0, 7)] public int facing;   // 0 = east, counter-clockwise in 45° steps
        public SpriteRenderer sprite;
        public Sprite[] rotations = new Sprite[0];
        public string id = "";

        float litUntil;
        public bool Lit => Time.time < litUntil;
        public Vector2 Facing => DirectionOf(facing);

        public static Vector2 DirectionOf(int step)
        {
            float a = step * 45f * Mathf.Deg2Rad;
            var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            return new Vector2(Mathf.Round(d.x * 1000f) / 1000f, Mathf.Round(d.y * 1000f) / 1000f).normalized;
        }

        void Start()
        {
            promptKey = "prompt.rotate";
            if (!string.IsNullOrEmpty(id) && Meta.GameSession.Instance != null)
            {
                // a turned crystal stays turned for the run
                for (int i = 0; i < 8; i++)
                    if (Meta.GameSession.Instance.HasFlag($"pedestal/{id}/{i}"))
                        facing = i;
            }
            Show();
        }

        public override void Interact(PlayerController player)
        {
            var session = Meta.GameSession.Instance;
            if (session != null && !string.IsNullOrEmpty(id))
                session.ClearFlag($"pedestal/{id}/{facing}");
            facing = (facing + 1) % 8;
            if (session != null && !string.IsNullOrEmpty(id))
                session.SetFlag($"pedestal/{id}/{facing}");
            Events.Play(SoundId.BeamHum, transform.position, 0.6f);
            Show();
        }

        /// <summary>Turns the crystal straight to a facing (puzzle setup and tests).</summary>
        public void SetFacing(int step)
        {
            facing = ((step % 8) + 8) % 8;
            Show();
        }

        void Show()
        {
            if (sprite != null && rotations != null && rotations.Length == 8)
                sprite.sprite = rotations[facing];
        }

        public bool Receive(BeamKind kind, Vector2 point, Vector2 direction, Vector2 normal, out Vector2 outOrigin, out Vector2 outDirection)
        {
            litUntil = Time.time + 0.2f;
            outDirection = Facing;
            outOrigin = (Vector2)transform.position + outDirection * 0.5f; // beams run through cell centres
            return true;
        }
    }
}
