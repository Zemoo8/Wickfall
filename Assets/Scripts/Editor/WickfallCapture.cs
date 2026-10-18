using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Wickfall.Meta;
using Wickfall.Player;

namespace Wickfall.EditorTools
{
    /// <summary>
    /// Renders still screenshots of generated scenes for docs and reviews (prompt §15: look at every
    /// visual before calling it done). Edit mode, so animation and runtime lighting are not shown;
    /// PlayMode captures come from the tests. Output: Docs/Screenshots/.
    /// Headless (needs a graphics device, so no -nographics):
    /// -batchmode -quit -executeMethod Wickfall.EditorTools.WickfallCapture.CaptureLightLab
    /// </summary>
    public static class WickfallCapture
    {
        public const string Output = "Docs/Screenshots";

        [MenuItem("WICKFALL/Tools/Capture Light Lab", priority = 10)]
        public static void CaptureLightLab()
        {
            var core = EditorSceneManager.OpenScene(SceneFactory.PathOf(SceneNames.Core), OpenSceneMode.Single);
            EditorSceneManager.OpenScene(SceneFactory.PathOf(SceneNames.LightLab, "Dev"), OpenSceneMode.Additive);
            var profile = Object.FindFirstObjectByType<SceneProfile>();
            var player = Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            var cam = Camera.main;
            if (player != null && profile != null && profile.playerStart != null)
                player.transform.position = profile.playerStart.position;

            Vector2 start = player != null ? (Vector2)player.transform.position : Vector2.zero;
            Shoot(cam, start + new Vector2(0f, 0.8f), 270f / 32f, "lightlab_start");
            Shoot(cam, new Vector2(24f, 9f), 10.5f, "lightlab_overview", 1920, 1080);

            // The player next to the effigies, so the character and the flame read at game scale.
            if (player != null)
            {
                player.transform.position = new Vector2(22f, 13.5f);
                Shoot(cam, new Vector2(22f, 14f), 270f / 32f, "lightlab_effigies");
            }
        }

        [MenuItem("WICKFALL/Tools/Capture Region 1", priority = 11)]
        public static void CaptureRegion1() => CaptureRegion(RegionId.TallowFields, new[]
        {
            // text coordinates of the level file (x, y down) and a name
            (18, 46, "start"), (44, 43, "clearing"), (76, 46, "geysers"), (74, 59, "shrine"), (50, 13, "herd"),
            (15, 9, "relic"), (95, 15, "brazier2"), (111, 33, "bell"),
        });

        public static void CaptureRegion2() => CaptureRegion(RegionId.Wickwood, new[]
        {
            (14, 62, "start"), (50, 60, "blooms"), (84, 58, "husks"), (54, 46, "lurkers"), (17, 44, "relic"),
            (84, 37, "vine"), (92, 25, "north"), (110, 22, "shrine"), (41, 14, "bell"),
        });

        public static void CaptureRegion(RegionId region, (int x, int y, string name)[] shots)
        {
            var core = EditorSceneManager.OpenScene(SceneFactory.PathOf(SceneNames.Core), OpenSceneMode.Single);
            string folder = RegionFactory.Folder(region);
            EditorSceneManager.OpenScene($"{folder}/{SceneNames.RegionBase(region)}.unity", OpenSceneMode.Additive);
            foreach (string path in System.IO.Directory.GetFiles(folder, "*.unity"))
                if (!path.Replace('\\', '/').EndsWith(SceneNames.RegionBase(region) + ".unity"))
                    EditorSceneManager.OpenScene(path.Replace('\\', '/'), OpenSceneMode.Additive);
            var director = Object.FindFirstObjectByType<World.RegionDirector>();
            var level = World.LevelData.Parse(director.levelFile.text);
            var player = Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            var cam = Camera.main;
            string key = "r" + ((int)region + 1);
            foreach (var (x, y, name) in shots)
            {
                Vector2 world = new Vector2(x + 0.5f, level.Height - 1 - y + 0.5f);
                if (player != null)
                    player.transform.position = world + new Vector2(0f, -1.5f);
                Shoot(cam, world, 270f / 32f, $"{key}_{name}");
            }
            // the whole region from above, lit as if it were day (layout check)
            var lights = Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None);
            var global = lights.FirstOrDefault(l => l.lightType == Light2D.LightType.Global);
            float before = global != null ? global.intensity : 0f;
            if (global != null) global.intensity = 0.9f;
            Shoot(cam, new Vector2(level.Width / 2f, level.Height / 2f), level.Height / 2f + 1f, $"{key}_map", 1920, 1080);
            if (global != null) global.intensity = before;
        }

        public static void Shoot(Camera cam, Vector2 center, float orthoSize, string name, int width = 960, int height = 540)
        {
            foreach (var canopy in Object.FindObjectsByType<Wickfall.Lighting.Canopy>(FindObjectsSortMode.None))
                canopy.Snap(center);
            Directory.CreateDirectory(Output);
            var pixel = cam.GetComponent<PixelPerfectCamera>();
            bool pixelWasOn = pixel != null && pixel.enabled;
            if (pixel != null)
                pixel.enabled = false; // the upscale pass assumes it draws to the screen (ECHO lesson)

            cam.orthographic = true;
            cam.orthographicSize = orthoSize;
            cam.transform.position = new Vector3(center.x, center.y, -10f);
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var previous = cam.targetTexture;
            cam.targetTexture = target;
            // Light2D builds its mesh and culling sphere in LateUpdate, which never runs for edit-mode
            // objects in batch mode: build them here, then warm the renderer up once.
            int lights = PrepareLights();
            cam.Render();
            cam.Render();
            Debug.Log($"[WICKFALL Capture] {name}: {lights} active 2D lights");
            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            RenderTexture.active = null;
            cam.targetTexture = previous;
            string path = $"{Output}/{name}.png";
            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
            if (pixel != null)
                pixel.enabled = pixelWasOn;
            Debug.Log("[WICKFALL Capture] " + path);
        }

        static readonly System.Reflection.MethodInfo UpdateMesh =
            typeof(Light2D).GetMethod("UpdateMesh", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        static readonly System.Reflection.MethodInfo UpdateBounds =
            typeof(Light2D).GetMethod("UpdateBoundingSphere", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        public static int PrepareLights()
        {
            int count = 0;
            foreach (var light in Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None))
            {
                if (!light.isActiveAndEnabled)
                    continue;
                count++;
                if (light.lightType == Light2D.LightType.Global)
                    continue;
                UpdateMesh?.Invoke(light, new object[] { true });
                UpdateBounds?.Invoke(light, null);
            }
            return count;
        }
    }
}
