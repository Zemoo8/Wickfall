using TMPro;
using UnityEngine;

namespace Wickfall.UI
{
    /// <summary>The "[E] Rest" prompt above whatever the player can use. The key glyph follows the last device used.</summary>
    public sealed class InteractPromptView : MonoBehaviour
    {
        public RectTransform root;
        public TMP_Text label;
        public Camera worldCamera;
        public RectTransform canvasRect;

        InteractPromptInfo info;
        float alpha;
        CanvasGroup group;

        void Awake()
        {
            group = root != null ? root.GetComponent<CanvasGroup>() : null;
        }

        void OnEnable()
        {
            Events.InteractPrompt.Register(OnPrompt);
            Events.LanguageChanged.Register(RefreshText);
        }

        void OnDisable()
        {
            Events.InteractPrompt.Unregister(OnPrompt);
            Events.LanguageChanged.Unregister(RefreshText);
        }

        void OnPrompt(InteractPromptInfo value)
        {
            info = value;
            RefreshText();
        }

        void RefreshText()
        {
            if (label == null || !info.Visible)
                return;
            string key = GameInput.Instance.LastDevice == InputDeviceKind.Gamepad ? "B" : "E";
            label.text = "[" + key + "] " + Loc.Get(info.TextKey);
        }

        void LateUpdate()
        {
            alpha = Mathf.MoveTowards(alpha, info.Visible ? 1f : 0f, Time.unscaledDeltaTime * 8f);
            if (group != null)
                group.alpha = alpha;
            if (root == null || alpha <= 0f)
                return;
            var cam = worldCamera != null ? worldCamera : Camera.main;
            if (cam == null || canvasRect == null)
                return;
            Vector2 screen = cam.WorldToScreenPoint(info.WorldPosition);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local))
                root.anchoredPosition = new Vector2(Mathf.Round(local.x), Mathf.Round(local.y));
        }
    }
}
