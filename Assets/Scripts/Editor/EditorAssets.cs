using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Wickfall.EditorTools
{
    /// <summary>Small helpers shared by the factories.</summary>
    public static class EditorAssets
    {
        public const string MaterialsFolder = "Assets/Materials";

        public static Material Material(string name, string shader)
        {
            EditorPaths.EnsureFolder(MaterialsFolder);
            string path = $"{MaterialsFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var s = Shader.Find(shader);
            if (s == null)
            {
                Debug.LogError($"[WICKFALL Setup] Shader '{shader}' not found.");
                return mat;
            }
            if (mat == null)
            {
                mat = new Material(s) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != s)
            {
                mat.shader = s;
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }

        public static Material Lit => Material("Mat_SpriteLitFlash", "Wickfall/SpriteLitFlash");
        public static Material LitDefault => AssetDatabase.LoadAssetAtPath<Material>(
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
        public static Material Glow => Material("Mat_SpriteGlow", "Wickfall/SpriteGlow");
        public static Material Flame => Material("Mat_FlameSprite", "Wickfall/FlameSprite");
        public static Material Reveal => Material("Mat_LightReveal", "Wickfall/LightReveal");
        public static Material Mirror => Material("Mat_MirrorSprite", "Wickfall/MirrorSprite");
        public static Material Unlit => AssetDatabase.LoadAssetAtPath<Material>(
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");

        public static GameObject Child(GameObject parent, string name, Vector3 local = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = local;
            return go;
        }

        public static SpriteRenderer Sprite(GameObject go, Sprite sprite, Material material, string sortingLayer, int order = 0)
        {
            if (!go.TryGetComponent(out SpriteRenderer sr))
                sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            if (material != null)
                sr.sharedMaterial = material;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = order;
            return sr;
        }

        public static Light2D PointLight(GameObject go, float radius, float intensity, Color color, bool shadows, int blendStyle = 0)
        {
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.pointLightOuterRadius = radius;
            light.pointLightInnerRadius = radius * 0.2f;
            light.intensity = intensity;
            light.color = color;
            light.shadowsEnabled = shadows;
            light.shadowIntensity = shadows ? 0.9f : 0f;
            light.blendStyleIndex = blendStyle;
            light.falloffIntensity = 0.6f;
            light.targetSortingLayers = AllSortingLayers();
            return light;
        }

        public static Light2D GlobalLight(GameObject go, float intensity, Color color)
        {
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = intensity;
            light.color = color;
            light.targetSortingLayers = AllSortingLayers();
            go.AddComponent<Wickfall.Lighting.AmbientBrightness>().baseIntensity = intensity; // the Brightness option (G69)
            return light;
        }

        public static int[] AllSortingLayers()
        {
            var layers = SortingLayer.layers;
            var ids = new int[layers.Length];
            for (int i = 0; i < layers.Length; i++)
                ids[i] = layers[i].id;
            return ids;
        }

        public static void SetLayer(GameObject go, string layer)
        {
            int index = LayerMask.NameToLayer(layer);
            if (index >= 0)
                go.layer = index;
        }

        public static T SavePrefab<T>(GameObject root, string path) where T : Component
        {
            EditorPaths.EnsureFolderOf(path);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab != null ? prefab.GetComponent<T>() : null;
        }

        public static GameObject SavePrefab(GameObject root, string path)
        {
            EditorPaths.EnsureFolderOf(path);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        public static VolumeProfile EnsureVolumeProfile(string path)
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile != null)
                return profile;
            EditorPaths.EnsureFolderOf(path);
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.85f);
            bloom.scatter.Override(0.62f);
            bloom.tint.Override(new Color(1f, 0.93f, 0.82f));
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.3f);
            vignette.smoothness.Override(0.45f);
            vignette.color.Override(new Color(0.055f, 0.051f, 0.047f));
            var grain = profile.Add<FilmGrain>(true);
            grain.intensity.Override(0.14f);
            grain.response.Override(0.7f);
            var adjustments = profile.Add<ColorAdjustments>(true);
            adjustments.contrast.Override(8f);
            adjustments.saturation.Override(-4f);
            foreach (var component in profile.components)
            {
                component.name = component.GetType().Name;
                AssetDatabase.AddObjectToAsset(component, profile);
            }
            EditorUtility.SetDirty(profile);
            return profile;
        }

        public static bool Exists(string path) => File.Exists(path);
    }
}
