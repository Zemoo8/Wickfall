using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wickfall.Meta;
using Wickfall.Presentation;

namespace Wickfall.UI
{
    /// <summary>
    /// The title screen: Continue / New game / options entries, navigated with the keyboard, mouse
    /// or gamepad. Buttons are built at runtime from localization keys, so both languages fit.
    /// </summary>
    public sealed class MainMenu : MonoBehaviour
    {
        public TMP_Text title;
        public TMP_FontAsset font;
        public float spacing = 16f;

        readonly List<(Button button, string key, Action action)> entries = new List<(Button, string, Action)>();
        RectTransform list;

        void Start()
        {
            list = SceneFactoryUi.Rect(gameObject, "Entries", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, 20f), new Vector2(200f, 120f));
            var session = GameSession.Instance;
            bool hasSave = GameServices.Instance != null && GameServices.Instance.Store.HasSave;
            if (hasSave)
                Add("menu.continue", Continue);
            Add("menu.newgame", NewGame);
            Add("menu.lightlab", () => SceneLoader.Instance.Load(SceneNames.LightLab));
            Add("menu.language", ToggleLanguage);
            Add("menu.quit", Quit);
            Layout();
            Events.LanguageChanged.Register(Refresh);
            if (entries.Count > 0)
                EventSystem.current?.SetSelectedGameObject(entries[0].button.gameObject);
        }

        void OnDestroy() => Events.LanguageChanged.Unregister(Refresh);

        void Add(string key, Action action)
        {
            var rt = SceneFactoryUi.Rect(list.gameObject, key, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(200f, 14f));
            var image = rt.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            var button = rt.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(1f, 0.76f, 0.3f, 0.12f);
            colors.selectedColor = new Color(1f, 0.76f, 0.3f, 0.18f);
            colors.pressedColor = new Color(1f, 0.76f, 0.3f, 0.3f);
            button.colors = colors;
            var labelRt = SceneFactoryUi.Rect(rt.gameObject, "Label", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 14f));
            var label = labelRt.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = 12f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.93f, 0.9f, 0.83f);
            label.raycastTarget = false;
            button.onClick.AddListener(() =>
            {
                Events.PlayUi(SoundId.UiConfirm);
                action();
            });
            entries.Add((button, key, action));
        }

        void Layout()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var rt = (RectTransform)entries[i].button.transform;
                rt.anchoredPosition = new Vector2(0f, -i * spacing);
            }
            Refresh();
        }

        void Refresh()
        {
            foreach (var (button, key, _) in entries)
            {
                var label = button.GetComponentInChildren<TMP_Text>();
                label.text = key == "menu.language" ? Loc.Get(key) + " : " + (Loc.IsFrench ? "Français" : "English") : Loc.Get(key);
            }
        }

        void Update()
        {
            // Keep a selection so keyboard and gamepad always work.
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null && entries.Count > 0)
                EventSystem.current.SetSelectedGameObject(entries[0].button.gameObject);
        }

        void Continue()
        {
            var session = GameSession.Instance;
            if (session != null && session.LoadGame())
                SceneLoader.Instance.Load(SceneNames.RegionBase((RegionId)session.Save.region));
        }

        void NewGame()
        {
            GameSession.Instance?.NewGame();
            string first = SceneNames.RegionBase(RegionId.TallowFields);
            SceneLoader.Instance.Load(Application.CanStreamedLevelBeLoaded(first) ? first : SceneNames.LightLab);
        }

        void ToggleLanguage()
        {
            string next = Loc.IsFrench ? Loc.English : Loc.French;
            if (GameServices.Instance != null) GameServices.Instance.SetLanguage(next);
            else Loc.SetLanguage(next);
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    /// <summary>RectTransform helper shared by runtime-built menus.</summary>
    public static class SceneFactoryUi
    {
        public static RectTransform Rect(GameObject parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.layer;
            go.transform.SetParent(parent.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }
    }
}
