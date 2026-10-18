using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Wickfall.UI;

namespace Wickfall.Meta
{
    /// <summary>
    /// PDF §4 SceneLoader: swaps the content scene (menu, region, showcase) with a fade while the
    /// Core scene stays loaded. Room groups inside a region are handled by RoomLoader.
    /// </summary>
    public sealed class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }
        public bool Busy { get; private set; }
        public string CurrentContent { get; private set; }

        readonly List<string> toUnload = new List<string>(8);

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Load(string scene, Action onLoaded = null, float fade = -1f)
        {
            if (Busy)
                return;
            StartCoroutine(LoadRoutine(scene, onLoaded, fade));
        }

        public IEnumerator LoadRoutine(string scene, Action onLoaded = null, float fade = -1f)
        {
            Busy = true;
            var fader = ScreenFader.Instance;
            if (fader != null && fade != 0f)
                yield return fader.FadeOut(fade);

            Time.timeScale = 1f;
            toUnload.Clear();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.name != SceneNames.Core && s.isLoaded)
                    toUnload.Add(s.name);
            }
            foreach (string name in toUnload)
            {
                var op = SceneManager.UnloadSceneAsync(name);
                if (op != null)
                    yield return op;
            }

            if (Application.CanStreamedLevelBeLoaded(scene))
            {
                yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
                var loaded = SceneManager.GetSceneByName(scene);
                if (loaded.IsValid())
                    SceneManager.SetActiveScene(loaded);
                CurrentContent = scene;
            }
            else
            {
                Debug.LogWarning($"[WICKFALL] Scene '{scene}' is not in the build; staying on Core.");
            }

            yield return null; // let the new scene's Start methods run
            onLoaded?.Invoke();
            Events.SceneReady.Raise();

            if (fader != null && fade != 0f)
                yield return fader.FadeIn(fade);
            Busy = false;
        }
    }
}
