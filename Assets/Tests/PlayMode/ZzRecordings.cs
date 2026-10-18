using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Wickfall.Meta;
using Wickfall.World;
using Debug = UnityEngine.Debug;

namespace Wickfall.Tests
{
    /// <summary>
    /// Not checks: gameplay videos for the presentation. A bot plays each region in showcase mode (no
    /// optional fights, sneaks, dodges) with a caption on top saying what it is doing. The game advances
    /// exactly 1/30 s per frame (Time.captureFramerate); every frame is rendered and piped to ffmpeg,
    /// which writes Builds/Videos/&lt;label&gt;.mp4 with the region's music.
    /// Run on demand: -Filter Wickfall.Tests.ZzRecordR1 (R1 … R6).
    /// </summary>
    public abstract class RecordingFixture : PlayModeFixture
    {
        const int Fps = 30;
        // exactly 3x the game's 480x270, so pixels and the HUD scale cleanly
        const int Width = 1440, Height = 810;
        const float IntroSeconds = 6f;
        const string HudText = "Bottom-left: the candle is the wax bar. Wax is life AND light: it burns down over time, and every hit costs wax. Next to it: the current flame";
        RenderTexture rt;
        Texture2D tex;
        Camera cam;
        PixelPerfectCamera pixel;
        Process ffmpeg;
        Stream pipe;
        byte[] buffer;
        bool pipeBroken;
        readonly StringBuilder ffmpegLog = new StringBuilder();
        int frame;
        string output;
        string language;
        RegionBotRunner.Run run;
        RectTransform captionBox;
        TextMeshProUGUI caption;
        string shown;
        string endCaption;
        bool narrationReady;

        protected abstract string Label { get; }
        protected abstract string Music { get; }
        /// <summary>Region number and Loc key (for the intro caption).</summary>
        protected abstract int Number { get; }
        protected abstract string RegionKey { get; }
        protected virtual string Goal => "Goal: ring the Great Bell to open the exit, and get out alive";
        protected virtual string Outro => "Region cleared: the score screen shows time, wax left, kills and rank";
        /// <summary>What this region adds, told after the HUD.</summary>
        protected abstract string RegionNote { get; }
        protected virtual string[] Route => null;
        /// <summary>Fights of the level's route kept in the video ("fight x y" prefixes): guards that cannot be walked past.</summary>
        protected virtual string[] KeepFights => null;
        /// <summary>Game speed in the video: a little faster than real, slow enough to follow the captions.</summary>
        protected virtual float Speed => 1.25f;

        static string En(string key) => UI.Loc.Get(key, UI.Loc.English) ?? key;

        string Intro => $"Region {Number}: {En(RegionKey)}. {En(RegionKey + ".env")}.\n{Goal} (bot playing at {Speed:0.##}x speed)";

        void Begin()
        {
            Directory.CreateDirectory("Builds/Videos");
            string oldFrames = Path.GetFullPath($"Builds/Videos/frames_{Label}"); // from the older JPEG recorder
            if (Directory.Exists(oldFrames))
                Directory.Delete(oldFrames, true);
            frame = 0;
            rt = new RenderTexture(Width, Height, 24);
            tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            buffer = new byte[Width * Height * 3];
            // the presentation look: brightness up (G69), English text (the explanations are in English)
            if (GameServices.Instance != null)
            {
                GameServices.Instance.Settings.brightness = 1f;
                Events.SettingsChanged.Raise();
            }
            language = UI.Loc.Language;
            UI.Loc.SetLanguage(UI.Loc.English);
            Time.captureFramerate = Fps;
            // a level opened directly (as here) is never faded in by the scene loader: clear the black
            UI.ScreenFader.Instance?.SetBlack(false);
            // recordings are not checks: unrelated error logs must not fail them
            LogAssert.ignoreFailingMessages = true;

            output = Path.GetFullPath($"Builds/Videos/{Label}.mp4");
            string music = Path.GetFullPath($"Assets/Audio/Music/{Music}.wav");
            // raw frames on stdin (bottom row first, hence vflip), the music looped under them
            string args = $"-y -loglevel error -f rawvideo -pix_fmt rgb24 -s {Width}x{Height} -framerate {Fps} -i - " +
                          $"-stream_loop -1 -i \"{music}\" -map 0:v -map 1:a -shortest -vf vflip " +
                          $"-c:v libx264 -preset veryfast -crf 25 -pix_fmt yuv420p -c:a aac -b:a 128k -movflags +faststart \"{output}\"";
            ffmpeg = Process.Start(new ProcessStartInfo("ffmpeg", args)
            {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardError = true,
            });
            ffmpeg.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (ffmpegLog) ffmpegLog.AppendLine(e.Data); };
            ffmpeg.BeginErrorReadLine();
            pipe = ffmpeg.StandardInput.BaseStream;
            pipeBroken = false;
        }

        /// <summary>Screen-space canvases are not drawn by cameras: show the HUD in camera space.</summary>
        void CanvasesToCamera()
        {
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!c.isRootCanvas || c.renderMode != RenderMode.ScreenSpaceOverlay)
                    continue;
                int order = c.sortingOrder;
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = 1f;
                // drawn above the whole world (on the default layer a full-screen panel would hide the floor)
                c.sortingLayerName = "Foreground";
                c.sortingOrder = 1000 + order;
                // the game's own UI scaler reads the batch window, not the video
                var pixelScaler = c.GetComponent<UI.PixelCanvasScaler>();
                if (pixelScaler != null) pixelScaler.enabled = false;
                var scaler = c.GetComponent<UnityEngine.UI.CanvasScaler>();
                if (scaler != null)
                    FitToVideo(scaler);
            }
        }

        /// <summary>Fits the 480x270 interface to the video's height, whatever size the batch window has.</summary>
        static void FitToVideo(UnityEngine.UI.CanvasScaler scaler)
        {
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(480f, 270f);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
        }

        /// <summary>The caption bar at the top of the video: what the bot is doing, in plain English.</summary>
        void BuildCaption()
        {
            var root = new GameObject("RecordingCaption", typeof(RectTransform));
            root.layer = LayerMask.NameToLayer("UI");
            Object.DontDestroyOnLoad(root); // the region scene unloads after its score screen
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 0.9f;
            canvas.sortingLayerName = "Foreground";
            canvas.sortingOrder = 3000;
            FitToVideo(root.AddComponent<UnityEngine.UI.CanvasScaler>());

            var box = new GameObject("Box", typeof(RectTransform));
            box.layer = root.layer;
            box.transform.SetParent(root.transform, false);
            captionBox = (RectTransform)box.transform;
            captionBox.anchorMin = captionBox.anchorMax = new Vector2(0.5f, 1f);
            captionBox.pivot = new Vector2(0.5f, 1f);
            captionBox.anchoredPosition = new Vector2(0f, -3f);
            captionBox.sizeDelta = new Vector2(420f, 24f);
            var back = box.AddComponent<UnityEngine.UI.Image>();
            back.color = new Color(0.055f, 0.051f, 0.047f, 0.82f);
            back.raycastTarget = false;

            var label = new GameObject("Text", typeof(RectTransform));
            label.layer = root.layer;
            label.transform.SetParent(box.transform, false);
            var rect = (RectTransform)label.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(6f, 2f);
            rect.offsetMax = new Vector2(-6f, -2f);
            caption = label.AddComponent<TextMeshProUGUI>();
            var gameText = Object.FindFirstObjectByType<TextMeshProUGUI>(FindObjectsInactive.Include);
            caption.font = gameText != null ? gameText.font : TMP_Settings.defaultFontAsset;
            caption.fontSize = 16f;
            caption.alignment = TextAlignmentOptions.Center;
            caption.enableWordWrapping = true;
            caption.color = new Color(0.93f, 0.9f, 0.83f);
            caption.raycastTarget = false;
            box.SetActive(false);
        }

        void ShowCaption(string text)
        {
            text ??= "";
            if (text == shown)
                return;
            shown = text;
            captionBox.gameObject.SetActive(text.Length > 0);
            caption.text = text;
            float height = caption.GetPreferredValues(text, captionBox.sizeDelta.x - 12f, 0f).y;
            captionBox.sizeDelta = new Vector2(captionBox.sizeDelta.x, Mathf.Ceil(height) + 6f);
        }

        void Capture()
        {
            if (cam == null)
            {
                cam = Camera.main;
                if (cam == null)
                    return;
                pixel = cam.GetComponent<PixelPerfectCamera>();
                if (pixel != null) pixel.enabled = false; // CLAUDE.md: off when rendering into a texture
                cam.cullingMask |= 1 << LayerMask.NameToLayer("UI");
                cam.targetTexture = rt; // for the whole recording, so the HUD lays out on the video's size
                // film grain is noise a video encoder cannot compress: off for the recording
                Lighting.Brightness.SetFilmGrain(false);
                BuildCaption();
                // the score screen stays up for the video's last seconds (the bot runner sets 0.5 s)
                UI.ScoreScreen.AutoContinueSeconds = 12f;
            }
            if (frame % 30 == 0)
                CanvasesToCamera();
            if (!narrationReady && run?.Bot != null)
            {
                // the intro first, then what the screen shows, then what this region adds
                narrationReady = true;
                run.Bot.HoldNarration(Mathf.Max(0f, IntroSeconds - frame / (float)Fps));
                run.Bot.Tell("hud", HudText, 5);
                run.Bot.Tell("region", RegionNote, 4);
            }
            ShowCaption(frame < Fps * IntroSeconds ? Intro : endCaption ?? run?.Bot?.Caption);
            Canvas.ForceUpdateCanvases();
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            RenderTexture.active = null;
            tex.GetPixelData<byte>(0).CopyTo(buffer);
            if (!pipeBroken)
            {
                try
                {
                    pipe.Write(buffer, 0, buffer.Length);
                }
                catch (IOException e)
                {
                    pipeBroken = true; // ffmpeg stopped: End() reports its log
                    Debug.LogWarning("[WICKFALL Video] ffmpeg pipe closed: " + e.Message);
                }
            }
            frame++;
        }

        void End()
        {
            Time.captureFramerate = 0;
            if (cam != null) cam.targetTexture = null;
            if (pixel != null) pixel.enabled = true;
            if (rt != null) { rt.Release(); Object.Destroy(rt); }
            if (tex != null) Object.Destroy(tex);
            if (language != null) UI.Loc.SetLanguage(language);
            try
            {
                pipe.Flush();
                pipe.Close();
            }
            catch (IOException) { }
            if (!ffmpeg.WaitForExit(180000))
                ffmpeg.Kill();
            Debug.Log($"[WICKFALL Video] {output}: {frame} frames, {frame / (float)Fps:0}s, ffmpeg exit {ffmpeg.ExitCode}");
            lock (ffmpegLog)
                if (ffmpegLog.Length > 0)
                    Debug.LogWarning("[WICKFALL Video] ffmpeg: " + ffmpegLog);
        }

        /// <summary>Not a check: a stray log while Core unloads must not mark the recording failed.</summary>
        [UnityTearDown]
        public IEnumerator IgnoreLogsOnTeardown()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return null;
        }

        protected IEnumerator Record()
        {
            yield return WaitUntil(() => RegionDirector.Instance != null && RoomLoader.Instance != null && RoomLoader.Instance.Settled, 10f);
            Begin();
            run = new RegionBotRunner.Run();
            var route = Route;
            if (route == null && KeepFights != null)
            {
                route = RegionDirector.Instance.Level.BotScript.ToArray();
                for (int i = 0; i < route.Length; i++)
                    foreach (var keep in KeepFights)
                        if (route[i].StartsWith(keep))
                            route[i] = "clear" + route[i].Substring("fight".Length);
            }
            yield return RegionBotRunner.Play(Player, Session, Label, run, 2400f, route, Speed, Capture, showcase: true);
            Time.timeScale = 1f;
            endCaption = Outro;
            for (int i = 0; i < Fps * 4; i++) // a few seconds after the end: the score screen or the ending
            {
                Capture();
                yield return null;
            }
            End();
            Assert.IsTrue(run.Bot.Done || run.Completed, run.Report);
            Assert.AreEqual(0, ffmpeg.ExitCode, "ffmpeg failed: " + ffmpegLog);
            Assert.IsTrue(File.Exists(output), output);
        }
    }

    public class ZzRecordR1 : RecordingFixture
    {
        protected override string SceneName => SceneNames.RegionBase(RegionId.TallowFields);
        protected override string Label => "Stage4_R1_TallowFields";
        protected override string Music => "TallowFields";
        protected override int Number => 1;
        protected override string RegionKey => "region.tallow";
        protected override string RegionNote => "The Tallow Fields teach the basics: walk, fight only when needed, rest at braziers, and find the region's flame and relic";
        [UnityTest, Timeout(3000000)] public IEnumerator Record_R1() => Record();
    }

    public class ZzRecordR2 : RecordingFixture
    {
        protected override string SceneName => SceneNames.RegionBase(RegionId.Wickwood);
        protected override string Label => "Stage4_R2_Wickwood";
        protected override string Music => "Wickwood";
        protected override int Number => 2;
        protected override string RegionKey => "region.wickwood";
        protected override string RegionNote => "Under the black canopy no light gets through: it is darker, but the creatures see you less there too";
        [UnityTest, Timeout(3000000)] public IEnumerator Record_R2() => Record();
    }

    public class ZzRecordR3 : RecordingFixture
    {
        protected override string SceneName => SceneNames.RegionBase(RegionId.SootFoundries);
        protected override string Label => "Stage5_R3_SootFoundries";
        protected override string Music => "SootFoundries";
        protected override int Number => 3;
        protected override string RegionKey => "region.foundries";
        protected override string RegionNote => "In the foundry, conveyors carry you, presses slam on a rhythm and steam jets scald: it is a timing puzzle";
        [UnityTest, Timeout(3000000)] public IEnumerator Record_R3() => Record();
    }

    public class ZzRecordR4 : RecordingFixture
    {
        protected override string SceneName => SceneNames.RegionBase(RegionId.GlassCathedral);
        protected override string Label => "Stage5_R4_GlassCathedral";
        protected override string Music => "GlassCathedral";
        protected override string[] Route => new[]
        {
            "rest 14 84", "read 12 83", "aim 20 81 2", "goto 29 74", "goto 29 69", "read 24 67", "goto 59 61", "goto 75 61",
            "take 86 61", "rest 74 64", "read 76 56", "smash 80 48", "flame 0", "read 76 41", "goto 80 40", "walk 80 32",
            "goto 60 31", "clear 42 30 13", "aim 42 24 6", "read 31 38", "drink 47 38", "rose 42 32", "goto 42 16",
            "read 32 11", "ring 42 9", "goto 42 6", "exit 42 2",
        };
        protected override int Number => 4;
        protected override string RegionKey => "region.cathedral";
        protected override string RegionNote => "In the cathedral, crystals steer a beam of light: it powers the rose window that opens the way on";
        [UnityTest, Timeout(3000000)] public IEnumerator Record_R4() => Record();
    }

    public class ZzRecordR5 : RecordingFixture
    {
        protected override string SceneName => SceneNames.RegionBase(RegionId.DeepHollow);
        protected override string Label => "Stage6_R5_DeepHollow";
        protected override string Music => "DeepHollow";
        protected override string[] KeepFights => new[] { "fight 52 30" }; // the Unlit around the relic
        protected override int Number => 5;
        protected override string RegionKey => "region.hollow";
        protected override string RegionNote => "No light at all here: only what the flame lights exists, so every creature stays hidden until it is close";
        [UnityTest, Timeout(3000000)] public IEnumerator Record_R5() => Record();
    }

    public class ZzRecordR6 : RecordingFixture
    {
        protected override string SceneName => SceneNames.RegionBase(RegionId.HearthChamber);
        protected override string Label => "Stage6_R6_Boss_and_EndingA";
        protected override string Music => "Boss";
        protected override int Number => 6;
        protected override string RegionKey => "region.hearth";
        protected override string RegionNote => "The last region: the Hollow King waits at the Hearth, where the whole journey leads";
        protected override string Goal => "Goal: beat the Hollow King, then choose an ending";
        protected override string Outro => "Ending A: the Grand Hearth burns again, and the Wickbearer is consumed by it";
        protected override string[] Route => new[]
        {
            "rest 34 82", "read 46 83", "goto 40 70", "read 40 59", "goto 14 54", "read 9 57", "take 7 54",
            "goto 40 55", "rest 63 55", "goto 40 46", "read 43 40", "boss 40 22", "judge 40 22 spare", "ending 40 8 A",
        };
        [UnityTest, Timeout(3000000)] public IEnumerator Record_R6() => Record();
    }
}
