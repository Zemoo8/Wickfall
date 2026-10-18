using UnityEngine;
using Wickfall.Meta;
using Wickfall.Player;

namespace Wickfall.World
{
    /// <summary>
    /// PDF §3 "Great Bell (hanging, ringing 4 frames)". G14: one at each region's exit gate. Ringing it
    /// opens the way on, but its toll carries across the region and wakes the swarm (PDF §2.5:
    /// "subir les conséquences (essaim)").
    /// </summary>
    public sealed class GreatBell : Interactable
    {
        public string gateId = "gate";
        public SpriteRenderer sprite;
        public Sprite stillSprite;
        public Sprite[] ringFrames = new Sprite[0];
        public float tollRadius = 40f;

        bool rung;
        float ringTime = -10f;

        public bool Rung => rung;

        void Start()
        {
            promptKey = "prompt.bell";
            rung = GameSession.Instance != null && GameSession.Instance.HasFlag("bell/" + gateId);
        }

        public override bool CanInteract(PlayerController player) => base.CanInteract(player) && !rung;

        public override void Interact(PlayerController player) => Ring();

        public void Ring()
        {
            if (rung)
                return;
            rung = true;
            ringTime = Time.time;
            GameSession.Instance?.SetFlag("bell/" + gateId);
            Events.Play(SoundId.BellRing, transform.position, 1f);
            Events.Shake(0.25f, 0.6f);
            // The toll is a flare heard across the region: every creature in range comes to look.
            Events.Flare.Raise(new FlareInfo(transform.position, tollRadius, FlareKind.BellRing, new Color(0.75f, 0.63f, 0.35f)));
            Gate.Find(gateId)?.Open();
            foreach (var swarm in SwarmNest.All)
                if (swarm.gateId == gateId)
                    swarm.Wake();
        }

        void Update()
        {
            if (sprite == null)
                return;
            float t = Time.time - ringTime;
            if (t < 2.4f && ringFrames.Length > 0)
                sprite.sprite = ringFrames[Mathf.FloorToInt(t * 6f) % ringFrames.Length];
            else if (stillSprite != null)
                sprite.sprite = stillSprite;
        }
    }
}
