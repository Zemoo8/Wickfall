using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Wickfall.Meta;
using Wickfall.Player;
using Wickfall.Presentation;
using Wickfall.World;

namespace Wickfall.DevTools
{
    /// <summary>
    /// PDF §4 "Pooling et Debug: outils de réglage de la détection". Backquote toggles it.
    /// Shows FPS, scene, the player's state machine, wax, light and detection radii, every
    /// creature's state; draws the light and detection circles in the world; sliders tune the
    /// detection formula live; cheats (infinite wax, all flames, teleport to braziers).
    /// Only in the editor and development builds, and only when the phase allows it.
    /// </summary>
    public sealed class DevOverlay : MonoBehaviour
    {
        public static readonly List<IDebugEntity> Entities = new List<IDebugEntity>(64);

        public Material lineMaterial;
        bool visible;
        bool showCircles = true;
        float fps;
        float fpsTimer;
        int frames;
        LineRenderer lightCircle, detectionCircle;
        GUIStyle style, boxStyle;
        Vector2 scroll;
        readonly System.Text.StringBuilder text = new System.Text.StringBuilder(1024);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Entities.Clear();

        public static bool Allowed =>
            (Application.isEditor || Debug.isDebugBuild) && PresentationMode.IsEnabled(Feature.DevOverlay);

        void Awake()
        {
            lightCircle = MakeCircle("Light radius", new Color(1f, 0.76f, 0.3f, 0.8f));
            detectionCircle = MakeCircle("Detection radius", new Color(1f, 0.35f, 0.3f, 0.8f));
            visible = PresentationMode.Phase == PresentationPhase.Testing; // Phase 6 shows the tools from the start
        }

        LineRenderer MakeCircle(string label, Color color)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = 64;
            line.widthMultiplier = 0.06f;
            line.startColor = line.endColor = color;
            line.material = lineMaterial;
            line.sortingLayerName = Layers.SortForeground;
            line.enabled = false;
            return line;
        }

        void Update()
        {
            frames++;
            fpsTimer += Time.unscaledDeltaTime;
            if (fpsTimer >= 0.5f)
            {
                fps = frames / fpsTimer;
                frames = 0;
                fpsTimer = 0f;
            }
            if (!Allowed)
            {
                visible = false;
            }
            else if (GameInput.Instance.DevOverlay.WasPressedThisFrame())
            {
                visible = !visible;
            }

            var player = GameSession.Instance != null ? GameSession.Instance.player : null;
            bool draw = visible && showCircles && player != null && player.isActiveAndEnabled;
            lightCircle.enabled = draw;
            detectionCircle.enabled = draw;
            if (draw)
            {
                var state = player.Light.State;
                Circle(lightCircle, state.Position, state.Radius);
                Circle(detectionCircle, state.Position, state.DetectionRadius);
            }
        }

        static void Circle(LineRenderer line, Vector2 center, float radius)
        {
            for (int i = 0; i < line.positionCount; i++)
            {
                float a = i / (float)line.positionCount * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(center.x + Mathf.Cos(a) * radius, center.y + Mathf.Sin(a) * radius, 0f));
            }
        }

        void OnGUI()
        {
            if (!visible)
                return;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true };
                style.normal.textColor = new Color(0.93f, 0.9f, 0.83f);
                boxStyle = new GUIStyle(GUI.skin.box);
            }

            var session = GameSession.Instance;
            var player = session != null ? session.player : null;
            text.Clear();
            text.Append("<b>WICKFALL dev</b>   ").Append(fps.ToString("0")).Append(" fps   ")
                .Append(SceneManager.GetActiveScene().name).Append("   ").Append(PresentationConfig.DisplayName(PresentationMode.Phase)).Append('\n');
            if (player != null)
            {
                var light = player.Light.State;
                text.Append("Player: <b>").Append(player.StateName).Append("</b>   flame ").Append(player.Flames.Current)
                    .Append(player.Dim.IsTrimmed ? "  (trimmed)" : "").Append('\n')
                    .Append("Wax ").Append(player.Wax.Current.ToString("0.0")).Append(" / ").Append(player.Wax.Max.ToString("0"))
                    .Append("   loss ").Append(player.Wax.LossPerSecond.ToString("0.00")).Append("/s\n")
                    .Append("Light radius ").Append(light.Radius.ToString("0.00")).Append("   detection ")
                    .Append(light.DetectionRadius.ToString("0.00")).Append("   (2 + r x 1.5)\n")
                    .Append("Dodge ").Append(player.Dodger.Charges).Append('/').Append(player.Dodger.MaxCharges)
                    .Append("   soot ").Append(player.Status.Sooted ? "yes" : "no")
                    .Append("   light cut ").Append(player.Status.LightCut ? player.Status.LightCutRemaining.ToString("0.0") : "no")
                    .Append("   dark zone ").Append(player.Status.InDarkness ? "yes" : "no").Append('\n');
            }
            text.Append("Creatures: ").Append(Entities.Count).Append('\n');
            for (int i = 0; i < Entities.Count && i < 14; i++)
                text.Append("  ").Append(Entities[i].DebugName).Append(": ").Append(Entities[i].DebugState).Append('\n');

            GUILayout.BeginArea(new Rect(10, 10, 470, Screen.height - 20), boxStyle);
            GUILayout.Label(text.ToString(), style);

            var cfg = Db.Light;
            GUILayout.Label("Detection = base + radius x factor", style);
            cfg.detectionBase = Slider("base", cfg.detectionBase, 0f, 6f);
            cfg.detectionPerRadius = Slider("factor", cfg.detectionPerRadius, 0f, 4f);
            showCircles = GUILayout.Toggle(showCircles, " circles (light / detection)");

            if (player != null && PresentationMode.IsEnabled(Feature.DebugCheats))
            {
                GUILayout.BeginHorizontal();
                player.Wax.GodMode = GUILayout.Toggle(player.Wax.GodMode, " infinite wax");
                if (GUILayout.Button("Refill")) player.Wax.Refill();
                if (GUILayout.Button("All flames"))
                    for (int f = 0; f < WickfallCounts.Flames; f++) session.UnlockFlame((FlameId)f);
                GUILayout.EndHorizontal();
                scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(90));
                foreach (var brazier in Brazier.All)
                    if (GUILayout.Button("Teleport: " + brazier.id))
                        player.Teleport(brazier.RespawnPosition);
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea();
        }

        float Slider(string label, float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + " " + value.ToString("0.00"), style, GUILayout.Width(110));
            value = GUILayout.HorizontalSlider(value, min, max);
            GUILayout.EndHorizontal();
            return value;
        }
    }

    /// <summary>Creatures register themselves so the overlay can list their state machines.</summary>
    public interface IDebugEntity
    {
        string DebugName { get; }
        string DebugState { get; }
    }
}
