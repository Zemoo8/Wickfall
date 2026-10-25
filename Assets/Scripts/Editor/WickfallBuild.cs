using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Wickfall.EditorTools
{
    /// <summary>
    /// WICKFALL > Build > Windows: a 64-bit Mono standalone at Builds/Windows/Wickfall.exe
    /// (Builds/ is not committed). Headless:
    /// -batchmode -quit -executeMethod Wickfall.EditorTools.WickfallBuild.BuildWindows [-development]
    /// WICKFALL > Build > Web (GamePix): a WebGL build at Builds/WebGL with the GamePix page template
    /// (G70); the editor goes back to Windows afterwards. Headless: ...WickfallBuild.BuildWebGL
    /// </summary>
    public static class WickfallBuild
    {
        public const string Output = "Builds/Windows/Wickfall.exe";
        public const string WebOutput = "Builds/WebGL";

        [MenuItem("WICKFALL/Build/Windows (release)", priority = 0)]
        public static void BuildWindowsMenu() => Build(false);

        [MenuItem("WICKFALL/Build/Windows (development)", priority = 1)]
        public static void BuildWindowsDev() => Build(true);

        public static void BuildWindows()
        {
            bool dev = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-development") >= 0;
            Build(dev);
        }

        static void Build(bool development)
        {
            SceneFactory.SetupBuildScenes();
            Directory.CreateDirectory(Path.GetDirectoryName(Output));
            var options = new BuildPlayerOptions
            {
                scenes = System.Array.ConvertAll(EditorBuildSettings.scenes, s => s.path),
                locationPathName = Output,
                target = BuildTarget.StandaloneWindows64,
                options = development ? BuildOptions.Development : BuildOptions.None,
            };
            Report(BuildPipeline.BuildPlayer(options), Output);
        }

        [MenuItem("WICKFALL/Build/Web (GamePix)", priority = 2)]
        public static void BuildWebGL()
        {
            SceneFactory.SetupBuildScenes();
            // GamePix: our own page (no Unity frame, its SDK first, a loading bar); Gzip with the
            // loader's fallback, so it plays even where the server does not mark the files as Gzip.
            PlayerSettings.WebGL.template = "PROJECT:GamePix";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = false;
            PlayerSettings.WebGL.showDiagnostics = false;
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            var options = new BuildPlayerOptions
            {
                scenes = System.Array.ConvertAll(EditorBuildSettings.scenes, s => s.path),
                locationPathName = WebOutput,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            Report(report, WebOutput);
        }

        static void Report(BuildReport report, string output)
        {
            var summary = report.summary;
            Debug.Log($"[WICKFALL Build] {summary.result}: {summary.totalSize / (1024 * 1024)} MB, {summary.totalErrors} errors, {summary.totalTime.TotalSeconds:0} s -> {output}");
            if (Application.isBatchMode && summary.result != BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }
    }
}
