using UnityEngine;

namespace Wickfall.Player
{
    /// <summary>
    /// Every animation of the Wickbearer (PDF §3: walk 4 directions x 6 frames; dim idle 4 frames;
    /// attack 5, dash 3, hurt 2, death 12). The flame is a separate layer, recoloured per flame at
    /// runtime, so one set of body frames serves all five flames. Swap art by editing this asset.
    /// </summary>
    [CreateAssetMenu(menuName = "WICKFALL/Visuals/Player Sprite Set", fileName = "PlayerSprites")]
    public sealed class PlayerSpriteSet : ScriptableObject
    {
        [Header("Indexed by Facing: Down, Up, Left, Right")]
        public SpriteClip[] idle = new SpriteClip[4];
        public SpriteClip[] walk = new SpriteClip[4];
        public SpriteClip[] attack = new SpriteClip[4];
        public SpriteClip[] dash = new SpriteClip[4];

        [Header("Single direction")]
        public SpriteClip dim;
        public SpriteClip hurt;
        public SpriteClip death;

        [Header("Flame layer (grey levels, recoloured by Wickfall/FlameSprite)")]
        public SpriteClip flame;
        public SpriteClip flameDim;
        [Tooltip("Where the flame sits, in pixels from the feet pivot, per Facing.")]
        public Vector2[] flameAnchor = { new Vector2(0, 33), new Vector2(0, 33), new Vector2(-1, 33), new Vector2(1, 33) };
        [Tooltip("Extra vertical offset of the flame for each walk frame (pixels).")]
        public float[] walkBob = { 0, -1, 0, 1, 0, -1 };
        public Vector2 dimFlameAnchor = new Vector2(0, 27);
        public int pixelsPerUnit = 16;

        public SpriteClip Idle(Facing f) => idle[(int)f];
        public SpriteClip Walk(Facing f) => walk[(int)f];
        public SpriteClip Attack(Facing f) => attack[(int)f];
        public SpriteClip Dash(Facing f) => dash[(int)f];
    }
}
