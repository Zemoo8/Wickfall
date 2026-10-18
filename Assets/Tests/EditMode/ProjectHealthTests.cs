using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Wickfall.Meta;

namespace Wickfall.Tests
{
    /// <summary>Guards against broken references in generated content (BUG_LOG: missing scripts).</summary>
    public class ProjectHealthTests
    {
        static int MissingScripts(GameObject root)
        {
            int missing = 0;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            return missing;
        }

        [Test]
        public void NoPrefab_HasAMissingScript()
        {
            var broken = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && MissingScripts(prefab) > 0)
                    broken.Add(path);
            }
            Assert.IsEmpty(broken, "prefabs with missing scripts: " + string.Join(", ", broken));
        }

        [Test]
        public void NoBuildScene_HasAMissingScript()
        {
            var broken = new List<string>();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (var entry in EditorBuildSettings.scenes.Where(s => s.enabled && File.Exists(s.path)))
                {
                    var scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
                    int missing = scene.GetRootGameObjects().Sum(MissingScripts);
                    if (missing > 0)
                        broken.Add($"{entry.path} ({missing})");
                }
            }
            finally
            {
                if (setup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
            Assert.IsEmpty(broken, "scenes with missing scripts: " + string.Join(", ", broken));
        }

        [Test]
        public void EveryMonoBehaviour_LivesInAFileOfTheSameName()
        {
            // Unity serializes a MonoBehaviour or ScriptableObject only if its file has the class name.
            var wrong = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/Scripts" }))
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid));
                var type = script != null ? script.GetClass() : null;
                if (type != null && !type.IsAbstract && !type.IsGenericType
                    && (typeof(MonoBehaviour).IsAssignableFrom(type) || typeof(ScriptableObject).IsAssignableFrom(type))
                    && type.Name != script.name)
                    wrong.Add($"{script.name}.cs declares {type.Name}");
            }
            // a script whose class is not found at all (GetClass() == null) while it declares a component
            foreach (var type in typeof(GameSession).Assembly.GetTypes())
            {
                if (type.IsAbstract || type.IsGenericType || !typeof(MonoBehaviour).IsAssignableFrom(type))
                    continue;
                var holder = MonoScriptFor(type);
                if (holder == null)
                    wrong.Add($"{type.FullName} has no file of its own");
            }
            Assert.IsEmpty(wrong, string.Join("; ", wrong));
        }

        static MonoScript MonoScriptFor(System.Type type)
        {
            foreach (string guid in AssetDatabase.FindAssets(type.Name + " t:MonoScript"))
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid));
                if (script != null && script.GetClass() == type)
                    return script;
            }
            return null;
        }
    }
}
