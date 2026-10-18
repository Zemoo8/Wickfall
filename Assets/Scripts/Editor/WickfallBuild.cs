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
    /// </summary>
    public static class WickfallBuild
    {
        public const string Output = "Builds/Windows/Wickfall.exe";

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
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[WICKFALL Build] {summary.result}: {summary.totalSize / (1024 * 1024)} MB, {summary.totalErrors} errors, {summary.totalTime.TotalSeconds:0} s -> {Output}");
            if (Application.isBatchMode && summary.result != BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }
    }
}
