using UnityEngine;

namespace Wickfall.Meta
{
    public enum SceneKind { Menu, Gameplay, Showcase, Cinematic }

    /// <summary>
    /// Tells the persistent Core what a content scene is: whether the player and HUD appear, which
    /// region it belongs to, where the player starts and which music plays.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public sealed class SceneProfile : MonoBehaviour
    {
        public SceneKind kind = SceneKind.Gameplay;
        public bool hasRegion = true;
        public RegionId region;
        public Transform playerStart;
        public MusicId music = MusicId.None;
        [Tooltip("Gameplay scenes that are sandboxes (Light Lab) never save.")]
        public bool sandbox;

        public static SceneProfile Active { get; private set; }

        void OnEnable() => Active = this;

        void OnDisable()
        {
            if (Active == this)
                Active = null;
        }

        void Start()
        {
            if (GameSession.Instance != null)
                GameSession.Instance.OnSceneProfile(this);
        }
    }
}
