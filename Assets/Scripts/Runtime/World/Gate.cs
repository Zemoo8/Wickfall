using System.Collections.Generic;
using UnityEngine;
using Wickfall.Meta;

namespace Wickfall.World
{
    /// <summary>
    /// PDF §3 "locked gate door (closed and open)". Blocks movement and navigation while closed;
    /// opened by its Great Bell (G14). Stays open for the run once opened.
    /// </summary>
    public sealed class Gate : MonoBehaviour
    {
        public string id = "gate";
        public SpriteRenderer sprite;
        public Sprite closedSprite;
        public Sprite openSprite;
        public Collider2D blocker;
        public int widthCells = 3;

        static readonly List<Gate> all = new List<Gate>();
        public static IReadOnlyList<Gate> All => all;
        public bool IsOpen { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        void Start()
        {
            bool open = GameSession.Instance != null && GameSession.Instance.HasFlag("gate/" + id);
            Apply(open, false);
        }

        public void Open()
        {
            if (IsOpen)
                return;
            GameSession.Instance?.SetFlag("gate/" + id);
            Apply(true, true);
        }

        void Apply(bool open, bool announce)
        {
            IsOpen = open;
            if (sprite != null)
                sprite.sprite = open ? openSprite : closedSprite;
            if (blocker != null)
                blocker.enabled = !open;
            var nav = NavGrid.Active;
            if (nav != null)
            {
                var cell = LevelData.CellOf(transform.position);
                for (int i = 0; i < widthCells; i++)
                    nav.SetBlocked(new Vector2Int(cell.x - widthCells / 2 + i, cell.y), !open);
            }
            if (announce)
            {
                Events.Play(SoundId.GateOpen, transform.position);
                Combat.Vfx.Play(Combat.VfxId.DustPuff, transform.position);
                Events.Shake(0.15f, 0.4f);
            }
        }

        public static Gate Find(string gateId)
        {
            foreach (var g in all)
                if (g.id == gateId)
                    return g;
            return null;
        }
    }
}
