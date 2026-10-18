using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Wickfall.Enemies;
using Wickfall.Lighting;
using Wickfall.Meta;
using Wickfall.Presentation;
using Wickfall.World;

namespace Wickfall.EditorTools
{
    /// <summary>
    /// Builds a region from its level file (Assets/Resources/Levels): a base scene (profile, global
    /// light, RegionDirector with the navigation grid, RoomLoader) and one scene per room group with
    /// its tilemaps and entities (PDF §4 RoomLoader: "chargement additif par groupe de salles").
    /// </summary>
    public static class RegionFactory
    {
        public static string Folder(RegionId region) => $"{SceneFactory.Folder}/Regions/R{(int)region + 1}";
        public static string GroupScene(RegionId region, string group) => $"{SceneNames.RegionBase(region)}_{group}";

        public static void EnsureAll()
        {
            Build(RegionId.TallowFields, "tallow");
        }

        public static void Build(RegionId region, string tileKey)
        {
            string levelPath = $"Assets/Resources/Levels/{SceneNames.RegionBase(region)}.txt";
            var levelAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(levelPath);
            if (levelAsset == null)
            {
                Debug.LogWarning("[WICKFALL Setup] No level file " + levelPath);
                return;
            }
            var level = LevelData.Parse(levelAsset.text);
            var tiles = AssetDatabase.LoadAssetAtPath<RegionTiles>($"Assets/Data/Tiles/{tileKey}/RegionTiles_{tileKey}.asset");
            var data = Db.Region(region);
            EditorPaths.EnsureFolder(Folder(region));
            string key = PdfTables.RegionKey(region);

            // ---- base scene -------------------------------------------------------------------
            string basePath = $"{Folder(region)}/{SceneNames.RegionBase(region)}.unity";
            if (File.Exists(basePath) && !IsGenerated(basePath))
                return;
            var scene = NewScene();
            new GameObject("CoreBootstrap").AddComponent<CoreBootstrap>();
            var profile = new GameObject("SceneProfile").AddComponent<SceneProfile>();
            profile.kind = SceneKind.Gameplay;
            profile.region = region;
            profile.music = data.music;
            var global = new GameObject("Global Light");
            EditorAssets.GlobalLight(global, data.ambientIntensity, data.ambientColor);
            var director = new GameObject("RegionDirector").AddComponent<RegionDirector>();
            director.region = region;
            director.levelFile = levelAsset;
            director.gameObject.AddComponent<PresentationGate>().feature = PresentationMode.RegionFeature(region);

            var start = level.Entities().FirstOrDefault(e => e.kind == "PlayerStart");
            var startGo = new GameObject("PlayerStart");
            startGo.transform.position = LevelData.CellCenter(start.cell);
            profile.playerStart = startGo.transform;

            var loader = new GameObject("RoomLoader").AddComponent<RoomLoader>();
            loader.groups = level.Groups.Select(g => new RoomLoader.RoomGroup { scene = GroupScene(region, g.Name), cells = g.Cells }).ToArray();
            EditorSceneManager.SaveScene(scene, basePath);

            // ---- room group scenes -------------------------------------------------------------
            var allEntities = level.Entities().ToList();
            var inscriptionOrder = allEntities.Where(e => e.kind == "Inscription").Select(e => e.cell).ToList();
            var braziers = allEntities.Where(e => e.kind == "Brazier").Select(e => e.cell).ToList();
            var pools = allEntities.Where(e => e.kind == "WaxPool").Select(e => e.cell).ToList();

            foreach (var group in level.Groups)
            {
                string groupPath = $"{Folder(region)}/{GroupScene(region, group.Name)}.unity";
                var gscene = NewScene();
                // A new scene unloads unused assets: load the tileset again after it (or every tile is a dead reference).
                tiles = AssetDatabase.LoadAssetAtPath<RegionTiles>($"Assets/Data/Tiles/{tileKey}/RegionTiles_{tileKey}.asset");
                var built = LevelBuilder.Build(null, "Level_" + group.Name, level.Rows, tiles, Vector2Int.zero, group.Cells,
                    data.lightRevealGround ? EditorAssets.Reveal : null, seed: group.Name.GetHashCode());
                var entities = new GameObject("Entities");
                var darkCells = new List<Vector2>();
                var gateCells = new List<Vector2Int>();

                foreach (var marker in built.Markers)
                {
                    string kind = level.KindAt(marker.Cell.x, marker.Cell.y);
                    if (kind == null)
                        continue;
                    Vector2 c = marker.Center;
                    string id = $"{key}/{kind}/{marker.Cell.x}_{marker.Cell.y}";
                    switch (kind)
                    {
                        case "PlayerStart":
                        case "Solid":
                            break;
                        case "Brazier":
                        {
                            var go = Place(PrefabFactory.BrazierPath, c + new Vector2(0f, -0.3f), entities);
                            go.GetComponent<Brazier>().id = $"{key}_B{braziers.IndexOf(marker.Cell) + 1}";
                            break;
                        }
                        case "WaxPool":
                        {
                            var go = Place(PrefabFactory.PoolPath, c, entities);
                            go.GetComponent<WaxPool>().id = $"{key}_W{pools.IndexOf(marker.Cell) + 1}";
                            break;
                        }
                        case "Geyser":
                        {
                            var go = Place(PrefabFactory.GeyserPath, c, entities);
                            go.GetComponent<WaxGeyser>().phaseOffset = ((marker.Cell.x * 7 + marker.Cell.y * 13) % 17) * 0.21f;
                            break;
                        }
                        case "Relic":
                            Place(WorldPrefabs.WorldPath("Relic"), c, entities).GetComponent<RelicPickup>().relicIndex = (int)region;
                            break;
                        case "FlameShrine":
                            Place(WorldPrefabs.WorldPath("FlameShrine"), c, entities).GetComponent<FlameShrine>().flame = data.flameFound;
                            break;
                        case "BrokenWick":
                            Place(WorldPrefabs.WorldPath("BrokenWick"), new Vector2(c.x + 0.5f, c.y - 1.5f), entities);
                            break;
                        case "GreatBell":
                            Place(WorldPrefabs.WorldPath("GreatBell"), c, entities).GetComponent<GreatBell>().gateId = key;
                            break;
                        case "Gate":
                            gateCells.Add(marker.Cell);
                            break;
                        case "Exit":
                            Place(WorldPrefabs.WorldPath("RegionExit"), c, entities);
                            break;
                        case "Inscription":
                        {
                            int n = inscriptionOrder.IndexOf(marker.Cell);
                            Place(WorldPrefabs.WorldPath("Inscription"), c, entities).GetComponent<Inscription>().textKey = $"inscription.r{(int)region + 1}.{n}";
                            break;
                        }
                        case "SwarmNest":
                        {
                            var nest = Place(WorldPrefabs.WorldPath("SwarmNest"), c, entities).GetComponent<SwarmNest>();
                            nest.gateId = key;
                            nest.id = id;
                            break;
                        }
                        case "DarkZone":
                            darkCells.Add(c);
                            break;
                        case "Cirelle":
                            Place(WorldPrefabs.WorldPath("Cirelle"), c, entities);
                            break;
                        default:
                        {
                            // Creatures: Assets/Prefabs/Enemies/<Kind>.prefab
                            string path = WorldPrefabs.EnemyPath(kind);
                            if (File.Exists(path))
                            {
                                var go = Place(path, c, entities);
                                if (go.TryGetComponent(out EnemyBrain brain))
                                    brain.entityId = id;
                            }
                            else
                            {
                                Debug.LogWarning($"[WICKFALL Setup] {key}: no prefab for '{kind}' at {marker.Cell}");
                            }
                            break;
                        }
                    }
                }

                // A gate spans its cells (one object at their centre, bottom row).
                if (gateCells.Count > 0)
                {
                    int minY = gateCells.Min(g => g.y);
                    var row = gateCells.Where(g => g.y == minY).ToList();
                    float cx = (row.Min(g => g.x) + row.Max(g => g.x) + 1) / 2f;
                    var gate = Place(WorldPrefabs.WorldPath("Gate"), new Vector2(cx, minY), entities).GetComponent<Gate>();
                    gate.id = key;
                    gate.widthCells = row.Count;
                }
                if (darkCells.Count > 0)
                    SceneFactoryDark(entities, darkCells);
                EditorSceneManager.SaveScene(gscene, groupPath);
            }
            Debug.Log($"[WICKFALL Setup] Built region {region}: {level.Groups.Count} room groups.");
        }

        static void SceneFactoryDark(GameObject parent, List<Vector2> cells)
        {
            // one trigger per contiguous horizontal run keeps the shape honest
            foreach (var row in cells.GroupBy(c => c.y))
            {
                var xs = row.Select(c => c.x).OrderBy(x => x).ToList();
                int start = 0;
                for (int i = 1; i <= xs.Count; i++)
                {
                    if (i == xs.Count || xs[i] - xs[i - 1] > 1.01f)
                    {
                        float x0 = xs[start] - 0.5f, x1 = xs[i - 1] + 0.5f;
                        var go = new GameObject("DarkZone");
                        go.transform.SetParent(parent.transform);
                        EditorAssets.SetLayer(go, Layers.Zone);
                        go.transform.position = new Vector2((x0 + x1) / 2f, row.Key);
                        var box = go.AddComponent<BoxCollider2D>();
                        box.isTrigger = true;
                        box.size = new Vector2(x1 - x0, 1f);
                        go.AddComponent<DarkZone>();
                        start = i;
                    }
                }
            }
        }

        static Scene NewScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("[Generated]").AddComponent<GeneratedSceneMarker>();
            return scene;
        }

        static bool IsGenerated(string path)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            return scene.GetRootGameObjects().Any(g => g.GetComponent<GeneratedSceneMarker>() != null);
        }

        static GameObject Place(string prefabPath, Vector2 position, GameObject parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[WICKFALL Setup] Missing prefab " + prefabPath);
                return new GameObject("Missing " + prefabPath);
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.scene);
            go.transform.SetParent(parent.transform, true);
            go.transform.position = position;
            return go;
        }
    }
}
