using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wickfall.Meta;
using Wickfall.Presentation;

namespace Wickfall.UI
{
    /// <summary>
    /// PDF §3 "pause menu frame in bone white on charcoal". Resume, options (language, sound,
    /// display, controls), back to the main menu, quit. Opens on the GamePaused channel.
    /// </summary>
    public sealed class PauseMenuView : MonoBehaviour
    {
        RectTransform root;
        readonly List<Button> buttons = new List<Button>();

        void OnEnable() => Events.GamePaused.Register(OnPaused);
        void OnDisable() => Events.GamePaused.Unregister(OnPaused);

        void OnPaused(bool paused)
        {
            if (paused) Open();
            else Close();
        }

        void Open()
        {
            if (root != null || UiKit.Instance == null || !PresentationMode.IsEnabled(Feature.PauseMenu))
                return;
            var kit = UiKit.Instance;
            root = UiKit.Fill(transform, "Pause");
            kit.Veil(root, 0.6f);
            var panel = kit.Panel(root, "Panel", new Vector2(200f, 150f));
            kit.Label(panel, "Title", Loc.Get("menu.paused"), new Vector2(0f, 56f), new Vector2(180f, 14f), TextAlignmentOptions.Center, UiKit.Amber);
            buttons.Clear();
            float y = 30f;
            buttons.Add(kit.Button(panel, "Resume", Loc.Get("menu.resume"), new Vector2(0f, y), new Vector2(170f, 14f), Resume)); y -= 16f;
            buttons.Add(kit.Button(panel, "Options", Loc.Get("menu.options"), new Vector2(0f, y), new Vector2(170f, 14f), OpenOptions)); y -= 16f;
            buttons.Add(kit.Button(panel, "Language", LanguageLabel(), new Vector2(0f, y), new Vector2(170f, 14f), ToggleLanguage)); y -= 16f;
            buttons.Add(kit.Button(panel, "Menu", Loc.Get("menu.mainmenu"), new Vector2(0f, y), new Vector2(170f, 14f), ToMenu)); y -= 16f;
            if (Application.platform != RuntimePlatform.WebGLPlayer) // a web page has no Quit (G70)
                buttons.Add(kit.Button(panel, "Quit", Loc.Get("menu.quit"), new Vector2(0f, y), new Vector2(170f, 14f), Quit));
            UiKit.Select(buttons[0]);
            Events.PlayUi(SoundId.UiOpen);
        }

        void Close()
        {
            if (root == null)
                return;
            Destroy(root.gameObject);
            root = null;
            OptionsView.Instance?.Close();
        }

        static string LanguageLabel() => Loc.Get("menu.language") + " : " + (Loc.IsFrench ? "Français" : "English");

        void Resume() => GameSession.Instance?.SetPaused(false);

        void OpenOptions() => OptionsView.Instance?.Open();

        void ToggleLanguage()
        {
            string next = Loc.IsFrench ? Loc.English : Loc.French;
            GameServices.Instance?.SetLanguage(next);
            Close();
            Open();
        }

        void ToMenu()
        {
            var session = GameSession.Instance;
            session?.SaveGame();
            session?.SetPaused(false);
            SceneLoader.Instance?.Load(SceneNames.MainMenu);
        }

        static void Quit()
        {
            GameSession.Instance?.SaveGame();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
