using UnityEngine;
using Wickfall.Meta;
using Wickfall.World;

namespace Wickfall.Lighting
{
    /// <summary>
    /// The forest canopy seen from above (PDF §2.2 "canopée qui bloque la lumière"): one overhead image
    /// of the region's leaf masses, drawn over everything. It turns see-through while the player is
    /// beneath it so the Wickbearer stays visible. Shade and hiding come from the DarkZones the region
    /// factory places under every canopy patch.
    /// </summary>
    public sealed class Canopy : MonoBehaviour
    {
        public SpriteRenderer leaves;
        /// <summary>The level's @canopy rows (text rows, y down; 'c' = leaves above that cell).</summary>
        public string[] rows = new string[0];
        public int levelHeight;
        [Range(0f, 1f)] public float closedAlpha = 0.94f;
        [Range(0f, 1f)] public float openAlpha = 0.3f;
        public float fadeSpeed = 2.5f;

        float alpha = 1f;

        public bool Covers(Vector2 world)
        {
            var cell = LevelData.CellOf(world);
            int row = levelHeight - 1 - cell.y;
            return row >= 0 && row < rows.Length && cell.x >= 0 && cell.x < rows[row].Length && rows[row][cell.x] == 'c';
        }

        /// <summary>Sets the fade at once, as if the player stood at `focus` (used by captures).</summary>
        public void Snap(Vector2 focus)
        {
            alpha = Covers(focus) ? openAlpha : closedAlpha;
            Apply();
        }

        void Start() => Snap(PlayerFocus(out _));

        Vector2 PlayerFocus(out bool hasPlayer)
        {
            var session = GameSession.Instance;
            var player = session != null ? session.player : null;
            hasPlayer = player != null && player.isActiveAndEnabled;
            return hasPlayer ? (Vector2)player.transform.position + new Vector2(0f, 0.8f) : new Vector2(-999f, -999f);
        }

        void LateUpdate()
        {
            Vector2 focus = PlayerFocus(out bool hasPlayer);
            bool under = hasPlayer && Covers(focus);
            alpha = Mathf.MoveTowards(alpha, under ? openAlpha : closedAlpha, Time.deltaTime * fadeSpeed);
            Apply();
        }

        void Apply()
        {
            if (leaves == null)
                return;
            var c = leaves.color;
            c.a = alpha;
            leaves.color = c;
        }
    }
}
