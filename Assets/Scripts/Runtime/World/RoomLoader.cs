using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Wickfall.Meta;

namespace Wickfall.World
{
    /// <summary>
    /// PDF §4 RoomLoader: "Chargement additif par groupe de salles, déchargement à distance."
    /// Each room group of a region is its own scene; groups near the player are loaded additively,
    /// far ones unloaded (with a margin between the two distances so nothing flickers).
    /// </summary>
    public sealed class RoomLoader : MonoBehaviour
    {
        [Serializable]
        public struct RoomGroup
        {
            public string scene;
            public RectInt cells;
        }

        public RoomGroup[] groups = new RoomGroup[0];
        public float loadDistance = 20f;
        public float unloadDistance = 30f;
        public float interval = 0.25f;

        readonly HashSet<string> loaded = new HashSet<string>();
        readonly HashSet<string> busy = new HashSet<string>();
        float next;

        public static RoomLoader Instance { get; private set; }
        public int LoadedCount => loaded.Count;
        public bool IsLoaded(string scene) => loaded.Contains(scene);
        public bool Settled => busy.Count == 0;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            // The SceneLoader unloads every content scene (room groups included) when the region changes.
            if (Instance == this)
                Instance = null;
            loaded.Clear();
        }

        static float Distance(Vector2 point, RectInt rect)
        {
            float dx = Mathf.Max(rect.xMin - point.x, 0f, point.x - rect.xMax);
            float dy = Mathf.Max(rect.yMin - point.y, 0f, point.y - rect.yMax);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        Vector2 Focus()
        {
            var session = GameSession.Instance;
            if (session != null && session.player != null && session.player.isActiveAndEnabled)
                return session.player.transform.position;
            var profile = SceneProfile.Active;
            return profile != null && profile.playerStart != null ? (Vector2)profile.playerStart.position : Vector2.zero;
        }

        void Update()
        {
            if (Time.unscaledTime < next)
                return;
            next = Time.unscaledTime + interval;
            Refresh(Focus());
        }

        /// <summary>Loads and unloads around a point now.</summary>
        public void Refresh(Vector2 focus)
        {
            for (int i = 0; i < groups.Length; i++)
            {
                var g = groups[i];
                float d = Distance(focus, g.cells);
                bool isLoaded = loaded.Contains(g.scene);
                if (!isLoaded && d <= loadDistance && !busy.Contains(g.scene))
                    Load(g.scene);
                else if (isLoaded && d >= unloadDistance && !busy.Contains(g.scene))
                    Unload(g.scene);
            }
        }

        void Load(string scene)
        {
            if (!Application.CanStreamedLevelBeLoaded(scene))
                return;
            var existing = SceneManager.GetSceneByName(scene);
            if (existing.isLoaded)
            {
                loaded.Add(scene);
                return;
            }
            busy.Add(scene);
            var op = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
            op.completed += _ =>
            {
                busy.Remove(scene);
                loaded.Add(scene);
            };
        }

        void Unload(string scene)
        {
            var s = SceneManager.GetSceneByName(scene);
            loaded.Remove(scene);
            if (!s.isLoaded)
                return;
            busy.Add(scene);
            var op = SceneManager.UnloadSceneAsync(s);
            if (op == null)
            {
                busy.Remove(scene);
                return;
            }
            op.completed += _ => busy.Remove(scene);
        }
    }
}
