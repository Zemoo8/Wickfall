using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Wickfall.Meta;
using Wickfall.Player;

namespace Wickfall.Tests
{
    /// <summary>
    /// Loads a real content scene (which loads Core), waits for the session and the player, and
    /// hands the player to a <see cref="ScriptedInput"/>. Sound is muted (ECHO lesson).
    /// </summary>
    public abstract class PlayModeFixture
    {
        protected PlayerController Player;
        protected GameSession Session;
        protected ScriptedInput Input;

        protected virtual string SceneName => SceneNames.LightLab;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            AudioListener.volume = 0f;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
            float waited = 0f;
            while ((GameSession.Instance == null || GameSession.Instance.player == null || !GameSession.Instance.player.isActiveAndEnabled
                    || GameSession.Instance.Profile == null) && waited < 10f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsNotNull(GameSession.Instance, "Core did not load");
            Session = GameSession.Instance;
            Player = Session.player;
            Input = new ScriptedInput();
            Player.SetInputSource(Input);
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Time.timeScale = 1f;
            if (Player != null)
                Player.SetInputSource(null);
            // Unload Core too, so every test starts from a clean Core.
            var core = SceneManager.GetSceneByName(SceneNames.Core);
            if (core.isLoaded)
                yield return SceneManager.UnloadSceneAsync(core);
            yield return null;
        }

        protected IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        protected IEnumerator WaitUntil(System.Func<bool> condition, float timeout = 5f)
        {
            float t = 0f;
            while (!condition() && t < timeout)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }
    }
}
