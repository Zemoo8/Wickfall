using UnityEngine;
using Wickfall.Combat;
using Wickfall.Meta;

namespace Wickfall.World
{
    /// <summary>
    /// PDF §2.3: "verre cassé par la cramoisie". A pane of black glass that only the crimson flame can
    /// break (the armor-like Glass tag); broken, it opens the path for the run. Beams pass through it.
    /// </summary>
    [RequireComponent(typeof(Damageable))]
    public sealed class GlassBarrier : MonoBehaviour
    {
        public string id = "";
        public SpriteRenderer sprite;
        public Collider2D solid;

        Damageable body;
        public bool Broken { get; private set; }

        void Awake()
        {
            body = GetComponent<Damageable>();
            body.Died += OnBroken;
        }

        void Start()
        {
            if (!string.IsNullOrEmpty(id) && GameSession.Instance != null && GameSession.Instance.HasFlag("glass/" + id))
                Break(false);
            else
                NavGrid.Active?.SetBlocked(LevelData.CellOf(transform.position), true);
        }

        void OnBroken(HitData hit) => Break(true);

        void Break(bool announce)
        {
            Broken = true;
            if (!string.IsNullOrEmpty(id))
                GameSession.Instance?.SetFlag("glass/" + id);
            if (solid != null)
                solid.enabled = false;
            foreach (var c in GetComponentsInChildren<Collider2D>())
                c.enabled = false;
            if (sprite != null)
                sprite.enabled = false;
            NavGrid.Active?.SetBlocked(LevelData.CellOf(transform.position), false);
            if (announce)
            {
                Events.Play(SoundId.GlassBreak, transform.position);
                Vfx.Play(VfxId.GlassShards, (Vector2)transform.position + new Vector2(0f, 0.5f));
                Events.Shake(0.08f, 0.2f);
            }
        }
    }
}
