using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Wickfall.Meta;
using Wickfall.UI;

namespace Wickfall.Presentation
{
    /// <summary>
    /// The first scene. Loads the persistent Core scene, then hands off to the active presentation
    /// phase's start scene (prompt §13): the concept deck, the design deck or the main menu.
    /// </summary>
    public sealed class Boot : MonoBehaviour
    {
        IEnumerator Start()
        {
            if (!SceneManager.GetSceneByName(SceneNames.Core).isLoaded)
                yield return SceneManager.LoadSceneAsync(SceneNames.Core, LoadSceneMode.Additive);
            yield return null;

            if (ScreenFader.Instance != null)
                ScreenFader.Instance.SetBlack(true);

            string start = PresentationMode.StartScene;
            if (!Application.CanStreamedLevelBeLoaded(start))
            {
                Debug.LogWarning($"[WICKFALL] Start scene '{start}' is not built yet; opening the main menu.");
                start = SceneNames.MainMenu;
            }
            Debug.Log($"[WICKFALL] Boot: {PresentationConfig.DisplayName(PresentationMode.Phase)} -> {start}");
            SceneLoader.Instance.Load(start, null, 0.6f);
        }
    }
}
