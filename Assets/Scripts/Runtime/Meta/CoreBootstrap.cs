using UnityEngine;
using UnityEngine.SceneManagement;

namespace Wickfall.Meta
{
    /// <summary>
    /// Put in every content scene: if the persistent Core scene (player, camera, systems, UI, audio)
    /// is not loaded yet - because Play was pressed in this scene directly - it loads it.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class CoreBootstrap : MonoBehaviour
    {
        void Awake()
        {
            if (!SceneManager.GetSceneByName(SceneNames.Core).isLoaded && GameServices.Instance == null)
                SceneManager.LoadScene(SceneNames.Core, LoadSceneMode.Additive);
        }
    }
}
