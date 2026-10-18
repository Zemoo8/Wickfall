using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Wickfall.Meta;
using Wickfall.UI;

namespace Wickfall.Tests
{
    /// <summary>
    /// Not checks: screenshots of the main menu and the options screen for Docs/Screenshots.
    /// Run on demand: -Filter Wickfall.Tests.ZzMenuShots
    /// </summary>
    public class ZzMenuShots
    {
        const int Width = 1440, Height = 810; // 3x the game's 480x270

        [UnityTest]
        public IEnumerator MainMenu_AndOptions()
        {
            PlayModeFixture.UseTestSaveFolder();
            AudioListener.volume = 0f;
            LogAssert.ignoreFailingMessages = true;
            SceneManager.LoadScene(SceneNames.MainMenu, LoadSceneMode.Single);
            MainMenu menu = null;
            for (float t = 0f; (menu == null || !menu.Lines.Any()) && t < 10f; t += Time.unscaledDeltaTime)
            {
                menu = Object.FindFirstObjectByType<MainMenu>();
                yield return null;
            }
            Assert.IsNotNull(menu, "the main menu");
            ScreenFader.Instance?.SetBlack(false);

            yield return Shoot("menu_main");
            OptionsView.Instance.Open();
            yield return Shoot("menu_options");
            OptionsView.Instance.Close();
        }

        /// <summary>Draws the screen-space canvases through the camera into a texture, then saves it.</summary>
        static IEnumerator Shoot(string name)
        {
            var cam = Camera.main;
            var pixel = cam.GetComponent<PixelPerfectCamera>();
            if (pixel != null) pixel.enabled = false; // CLAUDE.md: off when rendering into a texture
            cam.cullingMask |= 1 << LayerMask.NameToLayer("UI");
            var rt = new RenderTexture(Width, Height, 24);
            cam.targetTexture = rt;
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!c.isRootCanvas)
                    continue;
                if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    int order = c.sortingOrder; // keep the game's order between canvases
                    c.renderMode = RenderMode.ScreenSpaceCamera;
                    c.worldCamera = cam;
                    c.planeDistance = 1f;
                    c.sortingLayerName = "Foreground";
                    c.sortingOrder = 1000 + order;
                }
                var pixelScaler = c.GetComponent<PixelCanvasScaler>();
                if (pixelScaler != null) pixelScaler.enabled = false;
                var scaler = c.GetComponent<UnityEngine.UI.CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(480f, 270f);
                    scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    scaler.matchWidthOrHeight = 1f;
                }
            }
            for (int i = 0; i < 3; i++) // the scalers and the layout settle on the texture's size
                yield return null;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            RenderTexture.active = null;
            Directory.CreateDirectory("Docs/Screenshots");
            File.WriteAllBytes($"Docs/Screenshots/{name}.png", tex.EncodeToPNG());
            cam.targetTexture = null;
            if (pixel != null) pixel.enabled = true;
            rt.Release();
            Object.Destroy(rt);
            Object.Destroy(tex);
        }

        [UnityTearDown]
        public IEnumerator Unload()
        {
            LogAssert.ignoreFailingMessages = true;
            var core = SceneManager.GetSceneByName(SceneNames.Core);
            if (core.isLoaded)
                yield return SceneManager.UnloadSceneAsync(core);
            yield return null;
        }
    }
}
