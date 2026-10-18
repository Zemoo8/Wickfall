using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Wickfall.UI
{
    /// <summary>
    /// Builds the game's panels at runtime in one style (prompt §12: bone white on charcoal, the
    /// pixel font). Lives on the Core UI canvas.
    /// </summary>
    public sealed class UiKit : MonoBehaviour
    {
        public static UiKit Instance { get; private set; }

        public TMP_FontAsset font;
        public HudSprites sprites;
        public RectTransform overlay; // full-screen layer above the HUD for menus and cards

        public static readonly Color Bone = new Color(0.93f, 0.9f, 0.83f);
        public static readonly Color BoneDim = new Color(0.64f, 0.6f, 0.55f);
        public static readonly Color Amber = new Color(1f, 0.76f, 0.3f);
        public static readonly Color Charcoal = new Color(0.055f, 0.051f, 0.047f, 0.92f);

        /// <summary>A menu that used the pause key this frame sets this, so pause does not toggle too.</summary>
        public static int PauseConsumedFrame = -1;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Fill(Transform parent, string name)
        {
            var rt = Rect(parent, name, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            return rt;
        }

        public RectTransform Panel(Transform parent, string name, Vector2 size, Vector2 position = default)
        {
            var rt = Rect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprites != null ? sprites.panelFrame : null;
            image.type = Image.Type.Sliced;
            image.color = image.sprite != null ? Color.white : Charcoal;
            image.raycastTarget = true;
            return rt;
        }

        public TextMeshProUGUI Label(Transform parent, string name, string text, Vector2 position, Vector2 size,
            TextAlignmentOptions align = TextAlignmentOptions.Center, Color? color = null, float fontSize = 12f, bool wrap = false)
        {
            var rt = Rect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font;
            t.fontSize = fontSize;
            t.text = text;
            t.alignment = align;
            t.color = color ?? Bone;
            t.enableWordWrapping = wrap;
            t.raycastTarget = false;
            return t;
        }

        public Button Button(Transform parent, string name, string text, Vector2 position, Vector2 size, Action onClick)
        {
            var rt = Rect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            var button = rt.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(1f, 0.76f, 0.3f, 0.14f);
            colors.selectedColor = new Color(1f, 0.76f, 0.3f, 0.22f);
            colors.pressedColor = new Color(1f, 0.76f, 0.3f, 0.35f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0f);
            colors.fadeDuration = 0f;
            button.colors = colors;
            var label = Label(rt, "Label", text, Vector2.zero, size, TextAlignmentOptions.Center);
            button.onClick.AddListener(() =>
            {
                Events.PlayUi(SoundId.UiConfirm);
                onClick?.Invoke();
            });
            return button;
        }

        public static void Select(Selectable selectable)
        {
            if (selectable != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }

        /// <summary>A charcoal veil over the screen behind a menu.</summary>
        public Image Veil(Transform parent, float alpha = 0.6f)
        {
            var rt = Fill(parent, "Veil");
            var image = rt.gameObject.AddComponent<Image>();
            image.color = new Color(0.055f, 0.051f, 0.047f, alpha);
            image.raycastTarget = true;
            return image;
        }
    }
}
