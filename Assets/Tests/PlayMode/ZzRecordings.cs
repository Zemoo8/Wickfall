using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Wickfall.Meta;
using Wickfall.World;
using Debug = UnityEngine.Debug;

namespace Wickfall.Tests
{
    /// <summary>
    /// Not checks: gameplay videos for the presentation. A bot plays each region at normal speed, the
    /// game advances exactly 1/30 s per frame (Time.captureFramerate), every frame is rendered to a JPEG
    /// and ffmpeg turns them into Builds/Videos/&lt;region&gt;.mp4 with the region's music.
    /// Run on demand: -Filter Wickfall.Tests.ZzRecordings.R1 (or the whole class).
    /// </summary>
    public abstract class RecordingFixture : PlayModeFixture
    {
        const int Fps = 30;
        // exactly 3x the game's 480x270, so pixels and the HUD scale cleanly
        const int Width = 1440, Height = 810;
        string frames;
        int frame;
        RenderTexture rt;
        Texture2D tex;
        Camera cam;
        PixelPerfectCamera pixel;

        protected abstract string Label { get; }
        protected abstract string Music { get; }
        protected virtual string[] Route => null;
        /// <summary>Game speed in the video: 1.5 keeps each clip under two minutes.</summary>
        protected virtual float Speed => 1.5f;

        void Begin()
        {
            frames = Path.GetFullPath($"Builds/Videos/frames_{Label}");
            if (Directory.Exists(frames))
                Directory.Delete(frames, true);
            Directory.CreateDirectory(frames);
            frame = 0;
            rt = new RenderTexture(Width, Height, 24);
            tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            // the presentation look: brightness up (G69), so the world reads on a projector
            if (GameServices.Instance != null)
            {
                GameServices.Instance.Settings.brightness = 1f;
                Events.SettingsChanged.Raise();
            }
            Time.captureFramerate = Fps;
            // a level opened directly (as here) is never faded in by the scene loader: clear the black
            UI.ScreenFader.Instance?.SetBlack(false);
            // recordings are not checks: unrelated error logs must not fail them
            LogAssert.ignoreFailingMessages = true;
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
                {
                    // fit the 480x270 interface to the video's height, whatever size the batch window has
                    scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(480f, 270f);
                    scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    scaler.matchWidthOrHeight = 1f;
                }
            }
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
            }
            if (frame % 30 == 0)
                CanvasesToCamera();
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(frames, $"f{frame:00000}.jpg"), tex.EncodeToJPG(88));
            frame++;
        }

        void End()
        {
            Time.captureFramerate = 0;
            if (cam != null) cam.targetTexture = null;
            if (pixel != null) pixel.enabled = true;
            if (rt != null) { rt.Release(); Object.Destroy(rt); }
            if (tex != null) Object.Destroy(tex);
            string output = Path.GetFullPath($"Builds/Videos/{Label}.mp4");
            string music = Path.GetFullPath($"Assets/Audio/Music/{Music}.wav");
            string args = $"-y -framerate {Fps} -i \"{frames}\\f%05d.jpg\" -stream_loop -1 -i \"{music}\" " +
                          $"-map 0:v -map 1:a -shortest -c:v libx264 -pix_fmt yuv420p -crf 22 -preset medium -c:a aac -b:a 128k \"{output}\"";
            var ffmpeg = Process.Start(new ProcessStartInfo("ffmpeg", args) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true });
            string log = ffmpeg.StandardError.ReadToEnd();
            ffmpeg.WaitForExit();
            Debug.Log($"[WICKFALL Video] {output}: {frame} frames, {frame / (float)Fps:0}s, ffmpeg exit {ffmpeg.ExitCode}");
            if (ffmpeg.ExitCode == 0)
                Directory.Delete(frames, true);
            else
                Debug.LogWarning(log.Substring(System.Math.Max(0, log.Length - 1500)));
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
            var run = new RegionBotRunner.Run();
            yield return RegionBotRunner.Play(Player, Session, Label, run, 2400f, Route, Speed, Capture);
            Time.timeScale = 1f;
            for (int i = 0; i < Fps * 3; i++) // a few seconds after the end: the score screen or the ending
            {
                Capture();
                yield return null;
            }
            End();
            Assert.IsTrue(run.Bot.Done || run.Completed, run.Report);
        }
    }

    public class ZzRecordR1 : RecordingFixture
    {
        protected override string SceneName => SceneNames.RegionBase(RegionId.TallowFields);
        protected override string Label => "Stage4_R1_TallowFields";
        protected override string Music => "TallowFields";
        [UnityTest, Timeout(3000000)] public IEnumerator Record_R1() => Record();
    }

    public class ZzRecordR2 : RecordingFixture
    {
        protected override string SceneName => SceneNames.RegionBase(RegionId.Wickwood);
        protected override string Label => "Stage4_R2_Wickwood";
        protected override string Music => "Wickwood";
        [UnityTest, Timeout(3000000)] public IEnumerator Record_R2() => Record();
    }

    public class ZzRecordR3 : RecordingFixture
    {
        protected override string SceneName => SceneNames.RegionBase(RegionId.SootFoundries);
        protected override string Label => "Stage5_R3_SootFoundries";
        protected override string Music => "SootFoundries";
        [UnityTest, Timeout(3000000)] public IEnumerator Record_R3() => Record();
    }

    public class ZzRecordR4 : RecordingFixture
    {
        protected override string SceneName => SceneNames.RegionBase(RegionId.GlassCathedral);
        protected override string Label => "Stage5_R4_GlassCathedral";
        protected override string Music => "GlassCathedral";
        [UnityTest, Timeout(3000000)] public IEnumerator Record_R4() => Record();
    }

    public class ZzRecordR5 : RecordingFixture
    {
        protected override string SceneName => SceneNames.RegionBase(RegionId.DeepHollow);
        protected override string Label => "Stage6_R5_DeepHollow";
        protected override string Music => "DeepHollow";
        [UnityTest, Timeout(3000000)] public IEnumerator Record_R5() => Record();
    }

    public class ZzRecordR6 : RecordingFixture
    {
        protected override string SceneName => SceneNames.RegionBase(RegionId.HearthChamber);
        protected override string Label => "Stage6_R6_Boss_and_EndingA";
        protected override string Music => "Boss";
        protected override string[] Route => new[]
        {
            "rest 34 82", "read 46 83", "goto 40 70", "read 40 59", "goto 14 54", "read 9 57", "take 7 54",
            "goto 40 55", "rest 63 55", "goto 40 46", "read 43 40", "boss 40 22", "judge 40 22 spare", "ending 40 8 A",
        };
        [UnityTest, Timeout(3000000)] public IEnumerator Record_R6() => Record();
    }
}
