using UnityEngine;
using Wickfall.UI;

namespace Wickfall.Meta
{
    /// <summary>
    /// The persistent services of the Core scene: input bookkeeping, player settings (language,
    /// audio, display, controls), and the save store. Settings live in their own unencrypted file.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class GameServices : MonoBehaviour
    {
        public static GameServices Instance { get; private set; }

        public SaveStore Store { get; private set; }
        public SettingsData Settings { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Events.Hub.ResetAll();
            Store = SaveStore.Default;
            Settings = Store.LoadSettings();

            // First launch: follow the system language (French unless the system is English).
            if (string.IsNullOrEmpty(Settings.language))
                Settings.language = Loc.SystemDefault();
            Loc.SetLanguage(Settings.language, false);
            GameInput.Instance.LoadOverrides(Settings.bindingOverrides);
            ApplyDisplay();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Update() => GameInput.Instance.Update();

        void OnApplicationFocus(bool focused)
        {
            // Appendix A: the game pauses itself when the window loses focus.
            if (!focused && Application.isPlaying && !Application.isEditor)
                Events.GamePaused.Raise(true);
        }

        public void SaveSettings()
        {
            Settings.bindingOverrides = GameInput.Instance.SaveOverrides();
            Store.SaveSettings(Settings);
            Events.SettingsChanged.Raise();
        }

        public void SetLanguage(string code)
        {
            Settings.language = code;
            Loc.SetLanguage(code);
            SaveSettings();
        }

        public void ApplyDisplay()
        {
            if (Application.isEditor)
                return;
            QualitySettings.vSyncCount = Settings.vsync ? 1 : 0;
            Application.targetFrameRate = Settings.vsync ? -1 : Mathf.Max(30, Settings.frameCap);
            var mode = Settings.displayMode == 2 ? FullScreenMode.Windowed
                : Settings.displayMode == 1 ? FullScreenMode.FullScreenWindow : FullScreenMode.ExclusiveFullScreen;
            int w = Settings.resolutionWidth > 0 ? Settings.resolutionWidth : Display.main.systemWidth;
            int h = Settings.resolutionHeight > 0 ? Settings.resolutionHeight : Display.main.systemHeight;
            Screen.SetResolution(w, h, mode);
        }

        /// <summary>Volume for a group, including the master volume.</summary>
        public float Volume(AudioGroup group)
        {
            var s = Settings;
            if (s == null)
                return 1f;
            switch (group)
            {
                case AudioGroup.Music: return s.masterVolume * s.musicVolume;
                case AudioGroup.Ambience: return s.masterVolume * s.ambienceVolume;
                case AudioGroup.UI: return s.masterVolume * s.uiVolume;
                default: return s.masterVolume * s.sfxVolume;
            }
        }
    }

    public enum AudioGroup { Sfx, Music, Ambience, UI }
}
