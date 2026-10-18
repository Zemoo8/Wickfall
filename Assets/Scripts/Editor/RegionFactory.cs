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
            Build(RegionId.Wickwood, "wickwood");
            Build(RegionId.SootFoundries, "foundry");
            Build(RegionId.GlassCathedral, "cathedral");
            Build(RegionId.DeepHollow, "hollow");
            Build(RegionId.HearthChamber, "hearth");
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
            LevelBuilder.WallLikeChars = new HashSet<char>(level.Legend.Where(p => LevelData.WallKinds.Contains(p.Value)).Select(p => p.Key));
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

            BuildCanopyLayer(region, level);

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
                var gateCells = new Dictionary<string, List<Vector2Int>>(); // "Gate" (the bell's), "GateA", "GateB"...

                foreach (var marker in built.Markers)
                {
                    string kind = level.KindAt(marker.Cell.x, marker.Cell.y);
                    if (kind == null)
                        continue;
                    Vector2 c = marker.Center;
                    string id = $"{key}/{kind}/{marker.Cell.x}_{marker.Cell.y}";
                    if (kind.StartsWith("Gate"))
                    {
                        if (!gateCells.TryGetValue(kind, out var cellsOfGate))
                            gateCells[kind] = cellsOfGate = new List<Vector2Int>();
                        cellsOfGate.Add(marker.Cell);
                        continue;
                    }
                    if (kind.StartsWith("Pedestal"))
                    {
                        // "Pedestal3": a crystal pedestal first turned to facing 3 (x 45°, from east, counter-clockwise)
                        var pedestal = Place(WorldPrefabs.WorldPath("CrystalPedestal"), c, entities).GetComponent<CrystalPedestal>();
                        pedestal.facing = kind.Length > "Pedestal".Length ? int.Parse(kind.Substring("Pedestal".Length)) : 0;
                        pedestal.id = id;
                        continue;
                    }
                    if (kind.StartsWith("PactShrine"))
                    {
                        var shrine = Place(WorldPrefabs.WorldPath("PactShrine"), c, entities).GetComponent<PactShrine>();
                        shrine.pactIndex = int.Parse(kind.Substring("PactShrine".Length));
                        continue;
                    }
                    if (kind.StartsWith("Socket"))
                    {
                        var socket = Place(WorldPrefabs.WorldPath("BeamSocket"), c, entities).GetComponent<BeamSocket>();
                        socket.gateId = GateId(key, "Gate" + kind.Substring("Socket".Length));
                        socket.id = id;
                        continue;
                    }
                    if (kind.StartsWith("Rose"))
                    {
                        var rose = Place(WorldPrefabs.WorldPath("RoseWindow"), c, entities).GetComponent<RoseWindow>();
                        rose.gateId = GateId(key, "Gate" + kind.Substring("Rose".Length));
                        rose.id = id;
                        continue;
                    }
                    if (kind.StartsWith("Inscription"))
                    {
                        // "Inscription3" carries its number; a bare "Inscription" falls back to map order.
                        int n = kind.Length > "Inscription".Length
                            ? int.Parse(kind.Substring("Inscription".Length))
                            : inscriptionOrder.IndexOf(marker.Cell);
                        Place(WorldPrefabs.WorldPath("Inscription"), c, entities).GetComponent<Inscription>().textKey = $"inscription.r{(int)region + 1}.{n}";
                        continue;
                    }
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
                        case "ArenaPool":
                        {
                            var go = Place(PrefabFactory.PoolPath, c, entities);
                            var pool = go.GetComponent<WaxPool>();
                            pool.id = kind == "ArenaPool" ? $"{key}_arena_{marker.Cell.x}_{marker.Cell.y}" : $"{key}_W{pools.IndexOf(marker.Cell) + 1}";
                            pool.bossArena = kind == "ArenaPool"; // G13: dark from the King's first phase
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
                        case "Exit":
                            Place(WorldPrefabs.WorldPath("RegionExit"), c, entities);
                            break;
                        case "SwarmNest":
                        case "SwarmNestHusk":
                        case "SwarmNestLurker":
                        case "SwarmNestSlagborn":
                        case "SwarmNestBellows":
                        case "SwarmNestReflection":
                        case "SwarmNestUnlit":
                        {
                            var nest = Place(WorldPrefabs.WorldPath(kind), c, entities).GetComponent<SwarmNest>();
                            nest.gateId = key;
                            nest.id = id;
                            break;
                        }
                        case "Vine":
                        case "ConveyorE":
                        case "ConveyorW":
                        case "ConveyorN":
                        case "ConveyorS":
                            break; // built per run by BuildVines / BuildConveyors
                        case "Press":
                            Place(WorldPrefabs.WorldPath("Press"), c, entities).GetComponent<Press>().phaseOffset = PhaseFor(marker.Cell, 2.8f);
                            break;
                        case "SteamJetN":
                        case "SteamJetS":
                        case "SteamJetE":
                        case "SteamJetW":
                        {
                            var jet = Place(WorldPrefabs.WorldPath("SteamJet"), c, entities).GetComponent<SteamJet>();
                            jet.direction = DirectionOf(kind[kind.Length - 1]);
                            jet.length = 5;
                            jet.phaseOffset = PhaseFor(marker.Cell, 3.2f);
                            break;
                        }
                        case "Furnace":
                            Place(WorldPrefabs.WorldPath("Furnace"), c + new Vector2(0f, -0.4f), entities);
                            break;
                        case "BeamLampN":
                        case "BeamLampS":
                        case "BeamLampE":
                        case "BeamLampW":
                            Place(WorldPrefabs.WorldPath("BeamLamp"), c, entities).GetComponent<LightBeam>().direction = DirectionOf(kind[kind.Length - 1]);
                            break;
                        case "LaserN":
                        case "LaserS":
                        case "LaserE":
                        case "LaserW":
                        {
                            var laser = Place(WorldPrefabs.WorldPath("LaserEmitter"), c, entities).GetComponent<LaserEmitter>();
                            laser.direction = DirectionOf(kind[kind.Length - 1]);
                            laser.phaseOffset = PhaseFor(marker.Cell, 3.2f);
                            break;
                        }
                        case "MirrorSlash":
                        case "MirrorBackslash":
                            Place(WorldPrefabs.WorldPath(kind), c, entities);
                            break;
                        case "GlassBarrier":
                            Place(WorldPrefabs.WorldPath("GlassBarrier"), c, entities).GetComponent<GlassBarrier>().id = id;
                            break;
                        case "GlassFloor":
                            Place(WorldPrefabs.WorldPath("GlassFloor"), c, entities);
                            break;
                        case "Foyer":
                            Place(WorldPrefabs.WorldPath("GrandFoyer"), c, entities);
                            break;
                        case "Ash":
                            break; // built per run by BuildAsh
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
                                if (go.TryGetComponent(out LightEaterBloom bloom))
                                    bloom.entityId = id;
                            }
                            else
                            {
                                Debug.LogWarning($"[WICKFALL Setup] {key}: no prefab for '{kind}' at {marker.Cell}");
                            }
                            break;
                        }
                    }
                }

                // A gate spans its cells (one object at their centre, bottom row). "Gate" is opened by the
                // Great Bell (id = the region key); "GateA" etc. by puzzles (id = key/A).
                foreach (var pair in gateCells)
                {
                    int minY = pair.Value.Min(g => g.y);
                    var row = pair.Value.Where(g => g.y == minY).ToList();
                    float cx = (row.Min(g => g.x) + row.Max(g => g.x) + 1) / 2f;
                    var gate = Place(WorldPrefabs.WorldPath("Gate"), new Vector2(cx, minY), entities).GetComponent<Gate>();
                    gate.id = GateId(key, pair.Key);
                    gate.widthCells = row.Count;
                }
                if (darkCells.Count > 0)
                    SceneFactoryDark(entities, darkCells);
                BuildVines(level, tiles, group.Cells, built, entities);
                BuildConveyors(level, tiles, group.Cells, built, entities);
                BuildAsh(level, tiles, group.Cells, built, entities);
                BuildCanopyShade(level, group.Cells, entities);
                EditorSceneManager.SaveScene(gscene, groupPath);
            }
            LevelBuilder.WallLikeChars = new HashSet<char>();
            Debug.Log($"[WICKFALL Setup] Built region {region}: {level.Groups.Count} room groups.");
        }

        /// <summary>"Cendre qui ralentit": each horizontal run of Ash cells is one drift (pale ash tiles + an AshZone).</summary>
        static void BuildAsh(LevelData level, RegionTiles tiles, RectInt groupCells, LevelBuilder.Result built, GameObject parent)
        {
            var cells = level.Entities().Where(e => e.kind == "Ash" && groupCells.Contains(e.cell)).Select(e => e.cell).ToHashSet();
            if (cells.Count == 0)
                return;
            var ashTile = tiles.Special("ash").tile;
            foreach (var cell in cells)
            {
                built.Decor.SetTile(new Vector3Int(cell.x, cell.y, 0), ashTile);
                if (cells.Contains(cell + Vector2Int.left))
                    continue;
                int length = 1;
                while (cells.Contains(cell + Vector2Int.right * length))
                    length++;
                var go = new GameObject("AshDrift");
                go.transform.SetParent(parent.transform, false);
                go.transform.position = new Vector2(cell.x + length * 0.5f, cell.y + 0.5f);
                EditorAssets.SetLayer(go, Layers.Zone);
                var box = go.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(length, 1f);
                go.AddComponent<AshZone>().hazard = AssetDatabase.LoadAssetAtPath<HazardSO>(EditorPaths.Hazards + "/Hazard_Ash.asset");
            }
        }

        /// <summary>"Gate" belongs to the bell (the region key); "GateA" to a puzzle ("key/A").</summary>
        public static string GateId(string regionKey, string kind) =>
            kind == "Gate" ? regionKey : regionKey + "/" + kind.Substring("Gate".Length);

        static Vector2 DirectionOf(char letter) =>
            letter == 'N' ? Vector2.up : letter == 'S' ? Vector2.down : letter == 'E' ? Vector2.right : Vector2.left;

        /// <summary>A stable phase offset per cell, so neighbouring cyclic hazards do not fire together.</summary>
        static float PhaseFor(Vector2Int cell, float period) => ((cell.x * 37 + cell.y * 17) % 7) / 7f * period;

        /// <summary>
        /// Each straight run of conveyor cells (same direction, along that direction) becomes one belt:
        /// animated belt tiles plus a Conveyor that pushes whoever stands on it.
        /// </summary>
        static void BuildConveyors(LevelData level, RegionTiles tiles, RectInt groupCells, LevelBuilder.Result built, GameObject parent)
        {
            var cells = level.Entities().Where(e => e.kind.StartsWith("Conveyor")).ToDictionary(e => e.cell, e => e.kind[e.kind.Length - 1]);
            if (cells.Count == 0)
                return;
            foreach (var pair in cells)
            {
                var cell = pair.Key;
                char d = pair.Value;
                var along = (d == 'E' || d == 'W') ? Vector2Int.right : Vector2Int.up;
                if (cells.TryGetValue(cell - along, out char before) && before == d)
                    continue; // only start from the first cell of each run
                int length = 1;
                while (cells.TryGetValue(cell + along * length, out char next) && next == d)
                    length++;
                if (!groupCells.Contains(cell))
                    continue;
                var tile = tiles.Special("conveyor_" + char.ToLowerInvariant(d)).tile;
                for (int i = 0; i < length; i++)
                {
                    var p = cell + along * i;
                    built.Decor.SetTile(new Vector3Int(p.x, p.y, 0), tile);
                }
                var go = new GameObject("Conveyor" + d);
                go.transform.SetParent(parent.transform, false);
                Vector2 start = cell;
                Vector2 size = along == Vector2Int.right ? new Vector2(length, 1f) : new Vector2(1f, length);
                go.transform.position = start + size * 0.5f;
                EditorAssets.SetLayer(go, Layers.Zone);
                var box = go.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = size;
                var conveyor = go.AddComponent<Conveyor>();
                conveyor.direction = DirectionOf(d);
                conveyor.hazard = AssetDatabase.LoadAssetAtPath<HazardSO>(EditorPaths.Hazards + "/Hazard_Conveyor.asset");
            }
        }

        /// <summary>Each vertical run of Vine cells is one climbable vine: overlay tiles + a Vine at its foot.</summary>
        static void BuildVines(LevelData level, RegionTiles tiles, RectInt groupCells, LevelBuilder.Result built, GameObject parent)
        {
            var vineCells = level.Entities().Where(e => e.kind == "Vine").Select(e => e.cell).ToHashSet();
            if (vineCells.Count == 0)
                return;
            var top = tiles.Special("vine_top").tile;
            var mid = tiles.Special("vine_mid").tile;
            var bottom = tiles.Special("vine_bottom").tile;

            // The vines draw over the wall faces, on their own tilemap.
            var overlay = new GameObject("Vines");
            overlay.transform.SetParent(built.Root.transform, false);
            var map = overlay.AddComponent<UnityEngine.Tilemaps.Tilemap>();
            var mapRenderer = overlay.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();
            mapRenderer.sortingLayerName = Layers.SortEntities;
            mapRenderer.sortingOrder = 50;
            mapRenderer.sharedMaterial = EditorAssets.LitDefault;

            foreach (var cell in vineCells)
            {
                if (vineCells.Contains(cell + Vector2Int.up))
                    continue; // only start from the top cell of each run
                int length = 1;
                while (vineCells.Contains(cell + Vector2Int.down * length))
                    length++;
                var foot = cell + Vector2Int.down * (length - 1);
                if (!groupCells.Contains(foot))
                    continue;
                for (int i = 0; i < length; i++)
                {
                    var c = cell + Vector2Int.down * i;
                    map.SetTile(new Vector3Int(c.x, c.y, 0), i == 0 ? top : i == length - 1 ? bottom : mid);
                }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(WorldPrefabs.WorldPath("Vine")), parent.scene);
                go.transform.SetParent(parent.transform, true);
                go.transform.position = LevelData.CellCenter(foot) + Vector2.down;
                var vine = go.GetComponent<Vine>();
                vine.bottom = LevelData.CellCenter(foot + Vector2Int.down);
                vine.top = LevelData.CellCenter(cell + Vector2Int.up);
                var trigger = go.GetComponent<BoxCollider2D>();
                trigger.size = new Vector2(1.4f, length + 2.4f);
                trigger.offset = new Vector2(0f, (length + 1) * 0.5f);
            }
        }

        /// <summary>The canopy's leaf image (rendered by Tools/levels from the @canopy layer), over the whole map.</summary>
        static void BuildCanopyLayer(RegionId region, LevelData level)
        {
            string path = $"Assets/Art/Levels/{SceneNames.RegionBase(region)}_Canopy.png";
            if (!File.Exists(path))
                return;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); // the image is regenerated with the level
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null || level.Canopy.Length == 0)
                return;
            var go = new GameObject("Canopy");
            go.transform.position = new Vector3(level.Width / 2f, level.Height / 2f, 0f);
            var sr = EditorAssets.Sprite(go, sprite, EditorAssets.Lit, Layers.SortOverhead);
            var canopy = go.AddComponent<Canopy>();
            canopy.leaves = sr;
            canopy.rows = level.Canopy;
            canopy.levelHeight = level.Height;
        }

        /// <summary>
        /// Under the canopy (PDF §2.2): a shade (subtractive "negative" light) and a dark zone (detection
        /// halved, PDF §2.3) per horizontal run of canopy cells in the group.
        /// </summary>
        static void BuildCanopyShade(LevelData level, RectInt groupCells, GameObject parent)
        {
            var cells = new List<Vector2Int>();
            for (int y = groupCells.yMin; y < groupCells.yMax; y++)
                for (int x = groupCells.xMin; x < groupCells.xMax; x++)
                    if (level.HasCanopy(x, y) && level.IsWalkable(x, y))
                        cells.Add(new Vector2Int(x, y));
            if (cells.Count == 0)
                return;

            // shade and hiding per horizontal run
            foreach (var row in cells.GroupBy(c => c.y))
            {
                var xs = row.Select(c => c.x).OrderBy(x => x).ToList();
                int start = 0;
                for (int i = 1; i <= xs.Count; i++)
                {
                    if (i < xs.Count && xs[i] == xs[i - 1] + 1)
                        continue;
                    float x0 = xs[start], x1 = xs[i - 1] + 1;
                    var run = new GameObject("CanopyShade");
                    run.transform.SetParent(parent.transform, true);
                    run.transform.position = new Vector2((x0 + x1) / 2f, row.Key + 0.5f);
                    EditorAssets.SetLayer(run, Layers.Zone);
                    var box = run.AddComponent<BoxCollider2D>();
                    box.isTrigger = true;
                    box.size = new Vector2(x1 - x0, 1f);
                    run.AddComponent<DarkZone>();
                    var shade = run.AddComponent<Light2D>();
                    shade.lightType = Light2D.LightType.Freeform;
                    float hw = (x1 - x0) / 2f;
                    shade.SetShapePath(new[] { new Vector3(-hw, -0.5f), new Vector3(hw, -0.5f), new Vector3(hw, 0.5f), new Vector3(-hw, 0.5f) });
                    shade.blendStyleIndex = WickfallSetup.NegativeBlendStyle;
                    shade.intensity = 0.05f;
                    shade.falloffIntensity = 0.5f;
                    // the leaves are above the shade: only what lies beneath the canopy is darkened
                    shade.targetSortingLayers = EditorAssets.AllSortingLayers().Where(id => id != SortingLayer.NameToID(Layers.SortOverhead)).ToArray();
                    start = i;
                }
            }
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
