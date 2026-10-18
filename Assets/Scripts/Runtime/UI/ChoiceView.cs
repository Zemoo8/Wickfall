using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wickfall.Meta;

namespace Wickfall.UI
{
    /// <summary>Shows a pending <see cref="Choice"/> to a person: the game pauses until they answer.</summary>
    public sealed class ChoiceView : MonoBehaviour
    {
        RectTransform root;
        readonly List<Button> buttons = new List<Button>();
        bool paused;
        float timeScaleBefore = 1f;
        static ChoiceView shown;

        /// <summary>Gameplay videos: show the question even when a bot answers it (without pausing), so viewers see it.</summary>
        public static bool ShowForBots;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            ShowForBots = false;
            shown = null;
        }

        /// <summary>Moves the highlight to an option (a bot "pointing" at its answer in a video).</summary>
        public static void Highlight(int option)
        {
            if (shown != null && shown.root != null && option >= 0 && option < shown.buttons.Count)
                UiKit.Select(shown.buttons[option]);
        }

        void OnEnable()
        {
            Choice.Opened += Show;
            Choice.Closed += Hide;
        }

        void OnDisable()
        {
            Choice.Opened -= Show;
            Choice.Closed -= Hide;
        }

        void Show(ChoiceRequest request)
        {
            var session = GameSession.Instance;
            var player = session != null ? session.player : null;
            var kit = UiKit.Instance;
            bool bot = player != null && player.IsBotControlled;
            if (kit == null || (bot && !ShowForBots))
                return; // bots and tests answer through Choice.Resolve
            // the game waits for the answer, for a person and in a video alike
            if (!bot && player != null)
                player.ControlsLocked = true;
            paused = true;
            timeScaleBefore = Time.timeScale;
            Time.timeScale = 0f;
            shown = this;

            root = UiKit.Fill(transform, "Choice");
            kit.Veil(root, 0.6f);
            int count = request.OptionKeys.Length;
            float height = 70f + count * 16f;
            var panel = kit.Panel(root, "Panel", new Vector2(320f, height));
            kit.Label(panel, "Title", Loc.Get(request.TitleKey), new Vector2(0f, height / 2f - 16f), new Vector2(300f, 14f), TextAlignmentOptions.Center, UiKit.Amber);
            string body = request.BodyText ?? (string.IsNullOrEmpty(request.BodyKey) ? "" : Loc.Get(request.BodyKey));
            kit.Label(panel, "Body", body, new Vector2(0f, height / 2f - 40f), new Vector2(296f, 30f), TextAlignmentOptions.Center, UiKit.Bone, 12f, true);
            buttons.Clear();
            float y = height / 2f - 66f;
            for (int i = 0; i < count; i++)
            {
                int option = i;
                var b = kit.Button(panel, "Option" + i, Loc.Get(request.OptionKeys[i]), new Vector2(0f, y), new Vector2(260f, 14f), () => Choice.Resolve(option));
                b.interactable = request.IsEnabled(i);
                if (!b.interactable)
                    b.GetComponentInChildren<TMP_Text>().color = UiKit.BoneDim * 0.6f;
                buttons.Add(b);
                y -= 16f;
            }
            UiKit.Select(buttons.Find(b => b.interactable));
            Events.PlayUi(SoundId.UiOpen);
        }

        void Hide()
        {
            if (root != null)
                Destroy(root.gameObject);
            root = null;
            if (!paused)
                return;
            paused = false;
            Time.timeScale = timeScaleBefore > 0f ? timeScaleBefore : 1f;
            var session = GameSession.Instance;
            if (session != null && session.player != null)
                session.player.ControlsLocked = false;
        }
    }
}
