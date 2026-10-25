using System.Runtime.InteropServices;
using UnityEngine;
using Wickfall.World;

namespace Wickfall.Meta
{
    /// <summary>
    /// The web page around the WebGL build (GamePix, G70): tells it when the menu is up and when the
    /// first region starts, and takes its pause/resume calls (tab hidden, ads). It exists only in a
    /// WebGL player; the page side is Plugins/WebGL/WebHost.jslib and WebGLTemplates/GamePix (which
    /// also keeps the save folder in the browser's storage: autoSyncPersistentDataPath).
    /// </summary>
    public sealed class WebHost : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void WebHost_Loaded();
        [DllImport("__Internal")] static extern void WebHost_GameStart();
#else
        static void WebHost_Loaded() { }
        static void WebHost_GameStart() { }
#endif

        bool started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (Application.platform != RuntimePlatform.WebGLPlayer)
                return;
            // The page finds it by name (SendMessage), so the name must not change.
            var go = new GameObject(nameof(WebHost));
            DontDestroyOnLoad(go);
            go.AddComponent<WebHost>();
        }

        void Start() => WebHost_Loaded();

        void Update()
        {
            if (started || RegionDirector.Instance == null)
                return;
            started = true;
            WebHost_GameStart();
        }

        // Called by the page (SendMessage) when it hides the game or shows an ad.
        void HostPause()
        {
            Events.GamePaused.Raise(true);
            AudioListener.pause = true;
        }

        void HostResume()
        {
            // The game stays on its pause menu (it never resumes on its own); only the sound comes back.
            var session = GameSession.Instance;
            if (session == null || !session.Paused)
                AudioListener.pause = false;
        }
    }
}
