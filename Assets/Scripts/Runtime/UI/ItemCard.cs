using System;
using TMPro;
using UnityEngine;

namespace Wickfall.UI
{
    /// <summary>
    /// The card that appears when you take a relic, find a flame or read an inscription. Stays a few
    /// seconds (inscriptions until you walk away or press a key).
    /// </summary>
    public sealed class ItemCard : MonoBehaviour
    {
        public enum Kind { Relic, Flame, Inscription }

        /// <summary>Hook the world calls (null title = text only).</summary>
        public static Action<string, string, Kind> Show;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Show = null;

        CanvasGroup group;
        TextMeshProUGUI header, title, body;
        float hideAt;
        Vector2 shownAt;
        Kind kind;
        bool visible;

        void Start()
        {
            var kit = UiKit.Instance;
            if (kit == null)
                return;
            var panel = kit.Panel(transform, "Card", new Vector2(260f, 70f), new Vector2(0f, -70f));
            group = panel.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            header = kit.Label(panel, "Header", "", new Vector2(0f, 22f), new Vector2(240f, 14f), TextAlignmentOptions.Center, UiKit.BoneDim);
            title = kit.Label(panel, "Title", "", new Vector2(0f, 7f), new Vector2(240f, 14f), TextAlignmentOptions.Center, UiKit.Amber);
            body = kit.Label(panel, "Body", "", new Vector2(0f, -14f), new Vector2(240f, 28f), TextAlignmentOptions.Center, UiKit.Bone, 12f, true);
            Show = Open;
        }

        void OnDestroy()
        {
            if (Show == (Action<string, string, Kind>)Open)
                Show = null;
        }

        void Open(string titleKey, string bodyKey, Kind what)
        {
            if (group == null)
                return;
            kind = what;
            header.text = what == Kind.Relic ? Loc.Get("card.relic") : what == Kind.Flame ? Loc.Get("card.flame") : "";
            title.text = string.IsNullOrEmpty(titleKey) ? "" : Loc.Get(titleKey);
            body.text = Loc.Get(bodyKey);
            if (what == Kind.Inscription)
            {
                title.text = "";
                body.rectTransform.anchoredPosition = new Vector2(0f, 2f);
            }
            else
            {
                body.rectTransform.anchoredPosition = new Vector2(0f, -14f);
            }
            hideAt = Time.unscaledTime + (what == Kind.Inscription ? 30f : 4f);
            var session = Meta.GameSession.Instance;
            shownAt = session != null && session.player != null ? (Vector2)session.player.transform.position : Vector2.zero;
            visible = true;
        }

        void Update()
        {
            if (group == null)
                return;
            if (visible)
            {
                bool expired = Time.unscaledTime >= hideAt;
                var session = Meta.GameSession.Instance;
                if (kind == Kind.Inscription && session != null && session.player != null
                    && ((Vector2)session.player.transform.position - shownAt).sqrMagnitude > 2.5f)
                    expired = true;
                if (expired)
                    visible = false;
            }
            group.alpha = Mathf.MoveTowards(group.alpha, visible ? 1f : 0f, Time.unscaledDeltaTime * 5f);
        }
    }
}
