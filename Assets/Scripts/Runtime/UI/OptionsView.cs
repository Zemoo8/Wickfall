using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wickfall.Meta;

namespace Wickfall.UI
{
    /// <summary>
    /// Options (prompt §12): sound volumes, display, controls and accessibility. Each row cycles its
    /// value when pressed, so the whole screen works with the keyboard, mouse or a gamepad.
    /// Saved in the separate settings file.
    /// </summary>
    public sealed class OptionsView : MonoBehaviour
    {
        public static OptionsView Instance { get; private set; }

        RectTransform root;
        readonly List<(Button button, Func<string> label)> rows = new List<(Button, Func<string>)>();

        static readonly float[] Volumes = { 0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1f };
        static readonly float[] Intensities = { 0f, 0.25f, 0.5f, 0.75f, 1f };

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public bool IsOpen => root != null;

        public void Open()
        {
            var services = GameServices.Instance;
            if (root != null || UiKit.Instance == null || services == null)
                return;
            var s = services.Settings;
            var kit = UiKit.Instance;
            root = UiKit.Fill(transform, "Options");
            kit.Veil(root, 0.7f);
            var panel = kit.Panel(root, "Panel", new Vector2(300f, 236f));
            kit.Label(panel, "Title", Loc.Get("menu.options"), new Vector2(0f, 104f), new Vector2(280f, 14f), TextAlignmentOptions.Center, UiKit.Amber);
            rows.Clear();
            float y = 86f;

            void Row(string key, Func<string> value, Action change)
            {
                var b = kit.Button(panel, key, "", new Vector2(0f, y), new Vector2(270f, 14f), () =>
                {
                    change();
                    services.SaveSettings();
                    services.ApplyDisplay();
                    RefreshLabels();
                });
                rows.Add((b, () => Loc.Get(key) + " : " + value()));
                y -= 15f;
            }

            Row("options.master", () => Percent(s.masterVolume), () => s.masterVolume = Next(Volumes, s.masterVolume));
            Row("options.music", () => Percent(s.musicVolume), () => s.musicVolume = Next(Volumes, s.musicVolume));
            Row("options.sfx", () => Percent(s.sfxVolume), () => s.sfxVolume = Next(Volumes, s.sfxVolume));
            Row("options.ambience", () => Percent(s.ambienceVolume), () => s.ambienceVolume = Next(Volumes, s.ambienceVolume));
            Row("options.display", () => Loc.Get("options.display." + s.displayMode), () => s.displayMode = (s.displayMode + 1) % 3);
            Row("options.pixel", () => Loc.Get(s.pixelPerfect ? "options.pixel.on" : "options.pixel.off"), () => s.pixelPerfect = !s.pixelPerfect);
            Row("options.vsync", () => Loc.Get(s.vsync ? "options.on" : "options.off"), () => s.vsync = !s.vsync);
            Row("options.movement", () => Loc.Get(s.movementMode == 0 ? "options.movement.4" : "options.movement.8"), () =>
            {
                s.movementMode = 1 - s.movementMode;
                ApplyToPlayer(s);
            });
            Row("options.trim", () => Loc.Get(s.trimToggle ? "options.trim.toggle" : "options.trim.hold"), () =>
            {
                s.trimToggle = !s.trimToggle;
                ApplyToPlayer(s);
            });
            Row("options.shake", () => Percent(s.screenShake), () => s.screenShake = Next(Intensities, s.screenShake));
            Row("options.flash", () => Percent(s.flashIntensity), () => s.flashIntensity = Next(Intensities, s.flashIntensity));
            Row("options.language", () => Loc.IsFrench ? "Français" : "English", () =>
            {
                string next = Loc.IsFrench ? Loc.English : Loc.French;
                services.SetLanguage(next);
            });
            var back = kit.Button(panel, "Back", Loc.Get("menu.back"), new Vector2(0f, y - 6f), new Vector2(120f, 14f), Close);
            RefreshLabels();
            UiKit.Select(rows[0].button);
        }

        public static void ApplyToPlayer(SettingsData s)
        {
            var player = GameSession.Instance != null ? GameSession.Instance.player : null;
            if (player == null)
                return;
            player.Reader.Mode = (MovementMode)s.movementMode;
            player.Reader.TrimToggle = s.trimToggle;
            player.Reader.ResetToggles();
        }

        void RefreshLabels()
        {
            foreach (var (button, label) in rows)
                button.GetComponentInChildren<TMP_Text>().text = label();
        }

        public void Close()
        {
            if (root == null)
                return;
            Destroy(root.gameObject);
            root = null;
        }

        static string Percent(float v) => Mathf.RoundToInt(v * 100f) + " %";

        static float Next(float[] values, float current)
        {
            for (int i = 0; i < values.Length; i++)
                if (values[i] > current + 0.001f)
                    return values[i];
            return values[0];
        }

        void Update()
        {
            if (root != null && GameInput.Instance.Pause.WasPressedThisFrame())
            {
                UiKit.PauseConsumedFrame = Time.frameCount;
                Close();
            }
        }
    }
}
