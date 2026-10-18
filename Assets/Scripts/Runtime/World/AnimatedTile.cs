using UnityEngine;
using UnityEngine.Tilemaps;

namespace Wickfall.World
{
    /// <summary>
    /// A tile that cycles through frames (the 2-frame swaying wax grass, the 4-frame conveyor).
    /// Written here so the project does not need the Tilemap Extras package.
    /// </summary>
    [CreateAssetMenu(menuName = "WICKFALL/Visuals/Animated Tile", fileName = "AnimatedTile")]
    public sealed class AnimatedTile : TileBase
    {
        public Sprite[] frames = new Sprite[0];
        public float fps = 2f;
        public bool randomStart = true;
        public Tile.ColliderType collider = Tile.ColliderType.None;

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.sprite = frames.Length > 0 ? frames[0] : null;
            tileData.colliderType = collider;
            tileData.flags = TileFlags.LockTransform;
            tileData.color = Color.white;
            tileData.transform = Matrix4x4.identity;
        }

        public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData data)
        {
            if (frames.Length < 2)
                return false;
            data.animatedSprites = frames;
            data.animationSpeed = fps;
            data.animationStartTime = randomStart ? ((position.x * 7 + position.y * 13) % frames.Length) / Mathf.Max(0.01f, fps) : 0f;
            return true;
        }
    }
}
