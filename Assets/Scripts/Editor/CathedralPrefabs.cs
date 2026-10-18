using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Wickfall.Combat;
using Wickfall.Enemies;
using Wickfall.World;

namespace Wickfall.EditorTools
{
    /// <summary>
    /// Regions 4-5 prefabs: Reflections and the Unlit; the light puzzle (lamps, lasers, crystal
    /// pedestals, black mirrors, sockets, the Rose) and glass (barriers that crimson breaks, floors that
    /// crack under it). Beams run through cell centres, so every beam part is centred on its cell.
    /// </summary>
    public static class CathedralPrefabs
    {
        static Material BeamMaterial(string name, float intensity)
        {
            var mat = EditorAssets.Material(name, "Wickfall/SpriteGlow");
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Generated/VFX/Beam_Core.png");
            if (tex != null)
                mat.SetTexture("_MainTex", tex);
            mat.SetFloat("_Intensity", intensity);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static void EnsureAll()
        {
            WorldPrefabs.EnsureEnemy<ReflectionBrain>("Reflection", EnemyKind.Reflection, "Enemy_Reflection");
            WorldPrefabs.EnsureEnemy<UnlitBrain>("Unlit", EnemyKind.Unlit, "Enemy_Unlit");
            var light = BeamMaterial("Mat_Beam", 2.2f);
            var laser = BeamMaterial("Mat_Laser", 3.2f);
            EnsureLamp(light);
            EnsureLaser(laser);
            EnsurePedestal();
            EnsureMirror(true);
            EnsureMirror(false);
            EnsureSocket();
            EnsureRose();
            EnsureGlassBarrier();
            EnsureGlassFloor();
        }

        static BeamLine Line(GameObject parent, Material material, float width, Color color)
        {
            var go = EditorAssets.Child(parent, "Beam");
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = material;
            lr.sortingLayerName = Layers.SortVfx;
            lr.textureMode = LineTextureMode.Stretch;
            lr.alignment = LineAlignment.TransformZ;
            lr.numCapVertices = 0;
            lr.positionCount = 0;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var line = go.AddComponent<BeamLine>();
            line.width = width;
            line.color = color;
            return line;
        }

        static GameObject SolidBody(GameObject root, Vector2 size)
        {
            var solid = EditorAssets.Child(root, "Solid");
            EditorAssets.SetLayer(solid, Layers.Wall);
            solid.AddComponent<BoxCollider2D>().size = size;
            return solid;
        }

        static void EnsureLamp(Material material)
        {
            var root = new GameObject("BeamLamp");
            EditorAssets.Sprite(root, SpriteSheets.Clip(ArtFactory.Sheet("Props", "Prop_Lamp"), "lamp").FirstOrDefault(), EditorAssets.Lit, Layers.SortEntities);
            SolidBody(root, new Vector2(0.8f, 0.8f));
            EditorAssets.PointLight(EditorAssets.Child(root, "Glow"), 2f, 0.6f, new Color(0.93f, 0.9f, 0.83f), false);
            var beam = root.AddComponent<LightBeam>();
            beam.line = Line(root, material, 0.2f, new Color(0.93f, 0.9f, 0.83f, 0.95f));
            EditorAssets.SavePrefab(root, WorldPrefabs.WorldPath("BeamLamp"));
        }

        static void EnsureLaser(Material material)
        {
            var root = new GameObject("LaserEmitter");
            EditorAssets.Sprite(root, SpriteSheets.Clip(ArtFactory.Sheet("Props", "Prop_Lamp"), "lamp").ElementAtOrDefault(1), EditorAssets.Lit, Layers.SortEntities);
            SolidBody(root, new Vector2(0.8f, 0.8f));
            EditorAssets.PointLight(EditorAssets.Child(root, "Glow"), 1.6f, 0.5f, new Color(1f, 0.75f, 0.4f), false);
            var laser = root.AddComponent<LaserEmitter>();
            laser.hazard = AssetDatabase.LoadAssetAtPath<HazardSO>(EditorPaths.Hazards + "/Hazard_Laser.asset");
            laser.line = Line(root, material, 0.32f, new Color(1f, 0.86f, 0.6f, 1f));
            EditorAssets.SavePrefab(root, WorldPrefabs.WorldPath("LaserEmitter"));
        }

        static void EnsurePedestal()
        {
            var rotations = SpriteSheets.Clip(ArtFactory.Sheet("Props", "Prop_Pedestal"), "rot");
            var root = new GameObject("CrystalPedestal");
            EditorAssets.SetLayer(root, Layers.Interactable);
            root.AddComponent<SortingGroup>().sortingLayerName = Layers.SortEntities;
            var trigger = root.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 1f;
            var solid = EditorAssets.Child(root, "Solid");
            EditorAssets.SetLayer(solid, Layers.Wall);
            solid.AddComponent<CircleCollider2D>().radius = 0.42f;
            // the crystal (not the base) sits on the cell centre, where beams run
            var sr = EditorAssets.Sprite(EditorAssets.Child(root, "Visual", new Vector3(0f, -0.875f, 0f)), rotations.FirstOrDefault(), EditorAssets.Lit, Layers.SortEntities);
            var pedestal = root.AddComponent<CrystalPedestal>();
            pedestal.sprite = sr;
            pedestal.rotations = rotations;
            pedestal.promptOffset = new Vector2(0f, 1.2f);
            EditorAssets.SavePrefab(root, WorldPrefabs.WorldPath("CrystalPedestal"));
        }

        static void EnsureMirror(bool slash)
        {
            var frames = SpriteSheets.Clip(ArtFactory.Sheet("Props", "Prop_Mirror"), "mirror");
            var root = new GameObject(slash ? "MirrorSlash" : "MirrorBackslash");
            root.AddComponent<SortingGroup>().sortingLayerName = Layers.SortEntities;
            var solid = EditorAssets.Child(root, "Solid");
            EditorAssets.SetLayer(solid, Layers.Wall);
            solid.transform.localRotation = Quaternion.Euler(0f, 0f, slash ? 45f : -45f);
            solid.AddComponent<BoxCollider2D>().size = new Vector2(1.3f, 0.22f);
            EditorAssets.Sprite(EditorAssets.Child(root, "Visual", new Vector3(0f, -0.875f, 0f)), frames.ElementAtOrDefault(slash ? 0 : 1), EditorAssets.Lit, Layers.SortEntities);
            root.AddComponent<BlackMirror>().slash = slash;
            EditorAssets.SavePrefab(root, WorldPrefabs.WorldPath(root.name));
        }

        static void EnsureSocket()
        {
            var frames = SpriteSheets.Clip(ArtFactory.Sheet("Props", "Prop_Socket"), "socket");
            var root = new GameObject("BeamSocket");
            var sr = EditorAssets.Sprite(root, frames.FirstOrDefault(), EditorAssets.Lit, Layers.SortEntities);
            SolidBody(root, new Vector2(0.8f, 0.8f));
            var socket = root.AddComponent<BeamSocket>();
            socket.sprite = sr;
            socket.dark = frames.ElementAtOrDefault(0);
            socket.lit = frames.ElementAtOrDefault(1);
            socket.glow = EditorAssets.PointLight(EditorAssets.Child(root, "Glow"), 2.4f, 0.5f, new Color(0.93f, 0.9f, 0.83f), false);
            EditorAssets.SavePrefab(root, WorldPrefabs.WorldPath("BeamSocket"));
        }

        static void EnsureRose()
        {
            var root = new GameObject("RoseWindow");
            // laid in the cathedral floor: the eye (a socket the beam must reach) at its heart
            EditorAssets.Sprite(root, SpriteSheets.Clip(ArtFactory.Sheet("Props", "Prop_Rose"), "frame").FirstOrDefault(), EditorAssets.Lit, Layers.SortGroundDecal, 3);
            var eyeGo = EditorAssets.Child(root, "Eye");
            EditorAssets.SetLayer(eyeGo, Layers.Wall);
            eyeGo.AddComponent<CircleCollider2D>().radius = 0.55f;
            var eye = eyeGo.AddComponent<BeamSocket>();
            eye.glow = EditorAssets.PointLight(EditorAssets.Child(eyeGo, "Glow"), 2.2f, 0.5f, new Color(0.93f, 0.9f, 0.83f), false);
            var rose = root.AddComponent<RoseWindow>();
            rose.eye = eye;
            rose.glow = EditorAssets.PointLight(EditorAssets.Child(root, "Glow"), 4.5f, 0.1f, new Color(0.95f, 0.9f, 0.8f), false);
            var panelSprite = SpriteSheets.Clip(ArtFactory.Sheet("Props", "Prop_RosePanel"), "panel").FirstOrDefault();
            var flames = new[] { FlameId.Amber, FlameId.Blue, FlameId.Green, FlameId.White, FlameId.Crimson };
            rose.panels = new RosePanel[flames.Length];
            for (int k = 0; k < flames.Length; k++)
            {
                float a = (-90f + k * 72f) * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Cos(a), -Mathf.Sin(a), 0f) * 1.35f;
                var panelGo = EditorAssets.Child(root, "Panel_" + flames[k], pos);
                EditorAssets.SetLayer(panelGo, Layers.Interactable);
                var sr = EditorAssets.Sprite(panelGo, panelSprite, EditorAssets.Lit, Layers.SortGroundDecal, 4);
                var hurt = panelGo.AddComponent<CircleCollider2D>();
                hurt.isTrigger = true;
                hurt.radius = 0.55f;
                panelGo.AddComponent<Hurtbox>();
                var panel = panelGo.AddComponent<RosePanel>();
                panel.flame = flames[k];
                panel.sprite = sr;
                panel.glow = EditorAssets.PointLight(EditorAssets.Child(panelGo, "Glow"), 1.6f, 0.7f, Color.white, false);
                rose.panels[k] = panel;
            }
            EditorAssets.SavePrefab(root, WorldPrefabs.WorldPath("RoseWindow"));
        }

        static void EnsureGlassBarrier()
        {
            var root = new GameObject("GlassBarrier");
            EditorAssets.SetLayer(root, Layers.Breakable);
            root.AddComponent<SortingGroup>().sortingLayerName = Layers.SortEntities;
            var solid = root.AddComponent<BoxCollider2D>();
            solid.size = new Vector2(1f, 0.8f);
            solid.offset = new Vector2(0f, 0f);
            var hurt = EditorAssets.Child(root, "Hurtbox", new Vector3(0f, 0.5f, 0f));
            EditorAssets.SetLayer(hurt, Layers.Breakable);
            var hc = hurt.AddComponent<BoxCollider2D>();
            hc.isTrigger = true;
            hc.size = new Vector2(1f, 1.4f);
            hurt.AddComponent<Hurtbox>();
            var body = root.AddComponent<Damageable>();
            body.team = Team.Neutral;
            body.tags = DamageTags.Glass;
            body.maxHealth = 1f;
            body.knockbackResistance = 1f;
            var sr = EditorAssets.Sprite(EditorAssets.Child(root, "Visual", new Vector3(0f, -0.5f, 0f)),
                SpriteSheets.Clip(ArtFactory.Sheet("Props", "Prop_GlassBarrier"), "glass").FirstOrDefault(), EditorAssets.Lit, Layers.SortEntities);
            var glass = root.AddComponent<GlassBarrier>();
            glass.sprite = sr;
            glass.solid = solid;
            EditorAssets.SavePrefab(root, WorldPrefabs.WorldPath("GlassBarrier"));
        }

        static void EnsureGlassFloor()
        {
            var frames = SpriteSheets.Clip(ArtFactory.Sheet("Props", "Prop_GlassFloor"), "floor");
            var root = new GameObject("GlassFloor");
            var sr = EditorAssets.Sprite(root, frames.FirstOrDefault(), EditorAssets.Lit, Layers.SortGroundDecal, 2);
            var pitGo = EditorAssets.Child(root, "Pit");
            EditorAssets.SetLayer(pitGo, Layers.Wall);
            var pit = pitGo.AddComponent<BoxCollider2D>();
            pit.size = new Vector2(0.9f, 0.9f);
            pit.enabled = false;
            var floor = root.AddComponent<GlassFloor>();
            floor.hazard = AssetDatabase.LoadAssetAtPath<HazardSO>(EditorPaths.Hazards + "/Hazard_GlassFloor.asset");
            floor.sprite = sr;
            floor.intact = frames.ElementAtOrDefault(0);
            floor.crack1 = frames.ElementAtOrDefault(1);
            floor.crack2 = frames.ElementAtOrDefault(2);
            floor.hole = frames.ElementAtOrDefault(3);
            floor.pit = pit;
            EditorAssets.SavePrefab(root, WorldPrefabs.WorldPath("GlassFloor"));
        }
    }
}
