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
    /// The title screen: Continue / New game / Region select (G10) / Endings / Options / Language /
    /// Credits / Quit, navigated with the keyboard, mouse or gamepad. Buttons are built at runtime from
    /// localization keys, so both languages fit. Sub-pages replace the list and end with "Back".
    /// </summary>
    public sealed class MainMenu : MonoBehaviour
    {
        public TMP_Text title;
        public TMP_FontAsset font;
        public float spacing = 16f;

        readonly List<(Button button, string key, Action action)> entries = new List<(Button, string, Action)>();
        RectTransform list;
        TextMeshProUGUI note;
        SaveData record;
        Action rebuild;

        public enum Page { Main, Regions, Endings, Credits, HowTo }
        public Page Current { get; private set; }

        /// <summary>What the page shows: a key for menu entries, "=text" for lines (tests read this).</summary>
        public IEnumerable<(string key, bool interactable)> Lines
        {
            get
            {
                foreach (var e in entries)
                    yield return (e.key, e.button != null && e.button.interactable);
            }
        }

        System.Collections.IEnumerator Start()
        {
            // the save store lives in Core: when the menu is opened directly, Core may still be loading
            for (int i = 0; i < 300 && GameServices.Instance == null; i++)
                yield return null;
            list = SceneFactoryUi.Rect(gameObject, "Entries", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, 20f), new Vector2(240f, 140f));
            var noteRt = SceneFactoryUi.Rect(gameObject, "Note", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(460f, 14f));
            note = noteRt.gameObject.AddComponent<TextMeshProUGUI>();
            note.font = font;
            note.fontSize = 12f;
            note.alignment = TextAlignmentOptions.Center;
            note.color = new Color(0.64f, 0.6f, 0.55f);
            note.raycastTarget = false;
            if (GameServices.Instance != null && GameServices.Instance.Store.HasSave)
                GameServices.Instance.Store.TryLoad(out record);
            ShowMain();
            Events.LanguageChanged.Register(OnLanguage);
        }

        void OnDestroy() => Events.LanguageChanged.Unregister(OnLanguage);

        void OnLanguage() => rebuild?.Invoke();

        void Clear()
        {
            foreach (var (button, _, _) in entries)
                if (button != null)
                    Destroy(button.gameObject);
            entries.Clear();
            if (note != null)
                note.text = "";
        }

        public void ShowMain()
        {
            Clear();
            Current = Page.Main;
            rebuild = ShowMain;
            if (title != null) title.enabled = true;
            if (record != null)
                Add("menu.continue", Continue);
            Add("menu.newgame", NewGame);
            if (record != null && AnyCleared())
                Add("menu.regionselect", ShowRegions);
            if (record != null && record.endingsMask != 0)
                Add("menu.endings", ShowEndings);
            Add("menu.howto", ShowHowTo);
            Add("menu.options", () => OptionsView.Instance?.Open());
            Add("menu.language", ToggleLanguage);
            Add("menu.credits", ShowCredits);
            Add("menu.lightlab", () => SceneLoader.Instance.Load(SceneNames.LightLab));
            Add("menu.quit", Quit);
            Layout();
        }

        bool AnyCleared()
        {
            foreach (var r in record.regions)
                if (r.cleared)
                    return true;
            return false;
        }

        /// <summary>G10: replay any cleared region; its best score and rank are shown.</summary>
        public void ShowRegions()
        {
            Clear();
            Current = Page.Regions;
            rebuild = ShowRegions;
            if (title != null) title.enabled = false;
            for (int i = 0; i < WickfallCounts.Regions; i++)
            {
                var region = (RegionId)i;
                var r = record != null ? record.regions[i] : null;
                bool open = r != null && r.cleared;
                string label = open
                    ? $"{i + 1}. {Loc.Get(Db.Region(region).nameKey)}  -  {Loc.Get("rank." + ((Rank)Mathf.Max(0, r.bestRank)).ToString().ToLowerInvariant())} {Mathf.Max(0, r.bestScore)}"
                    : $"{i + 1}. ---";
                var b = AddLabel(label, () => Replay(region));
                b.interactable = open;
            }
            Add("menu.back", ShowMain);
            Layout();
            note.text = Loc.Get("menu.regionselect.note");
        }

        void Replay(RegionId region)
        {
            var session = GameSession.Instance;
            if (session == null || !session.LoadGame())
                return;
            SceneLoader.Instance.Load(SceneNames.RegionBase(region));
        }

        /// <summary>The endings found so far (PDF §1: A, B and the secret C).</summary>
        public void ShowEndings()
        {
            Clear();
            Current = Page.Endings;
            rebuild = ShowEndings;
            if (title != null) title.enabled = false;
            for (int i = 0; i < 3; i++)
            {
                var id = (EndingId)i;
                bool found = record != null && record.HasEnding(id);
                string label = found ? Loc.Get($"ending.{id.ToString().ToLowerInvariant()}.title") : Loc.Get("ending.locked");
                AddLabel(label, null).interactable = false;
            }
            Add("menu.back", ShowMain);
            Layout();
        }

        public void ShowCredits()
        {
            Clear();
            Current = Page.Credits;
            rebuild = ShowCredits;
            if (title != null) title.enabled = false;
            for (int i = 0; i < 8; i++)
                AddLabel(Loc.Get("credits." + i), null).interactable = false;
            Add("menu.back", ShowMain);
            Layout();
        }

        /// <summary>How to play: the controls and the one rule (wax = life = light = risk).</summary>
        public void ShowHowTo()
        {
            Clear();
            Current = Page.HowTo;
            rebuild = ShowHowTo;
            if (title != null) title.enabled = false;
            for (int i = 0; i < 9; i++)
                AddLabel(Loc.Get("howto." + i), null).interactable = false;
            Add("menu.back", ShowMain);
            Layout();
        }

        Button AddLabel(string text, Action action)
        {
            Add("", action ?? (() => { }));
            var e = entries[entries.Count - 1];
            entries[entries.Count - 1] = (e.button, "=" + text, e.action);
            return e.button;
        }

        void Add(string key, Action action)
        {
            var rt = SceneFactoryUi.Rect(list.gameObject, string.IsNullOrEmpty(key) ? "Line" : key, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(240f, 14f));
            var image = rt.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            var button = rt.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(1f, 0.76f, 0.3f, 0.12f);
            colors.selectedColor = new Color(1f, 0.76f, 0.3f, 0.18f);
            colors.pressedColor = new Color(1f, 0.76f, 0.3f, 0.3f);
            button.colors = colors;
            var labelRt = SceneFactoryUi.Rect(rt.gameObject, "Label", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 14f));
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
            float top = Current == Page.Main ? 0f : 40f;
            for (int i = 0; i < entries.Count; i++)
            {
                var rt = (RectTransform)entries[i].button.transform;
                rt.anchoredPosition = new Vector2(0f, top - i * spacing);
            }
            Refresh();
            var first = entries.Find(e => e.button.interactable);
            if (first.button != null)
                EventSystem.current?.SetSelectedGameObject(first.button.gameObject);
        }

        void Refresh()
        {
            foreach (var (button, key, _) in entries)
            {
                var label = button.GetComponentInChildren<TMP_Text>();
                if (key.StartsWith("="))
                    label.text = key.Substring(1);
                else
                    label.text = key == "menu.language" ? Loc.Get(key) + " : " + (Loc.IsFrench ? "Français" : "English") : Loc.Get(key);
                label.color = button.interactable || key.StartsWith("=") ? new Color(0.93f, 0.9f, 0.83f) : new Color(0.45f, 0.42f, 0.38f);
            }
        }

        void Update()
        {
            // Keep a selection so keyboard and gamepad always work.
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null && entries.Count > 0)
            {
                var first = entries.Find(e => e.button.interactable);
                if (first.button != null)
                    EventSystem.current.SetSelectedGameObject(first.button.gameObject);
            }
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
