using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wickfall.Meta;

namespace Wickfall.UI
{
    /// <summary>
    /// The Ending scene (PDF §1): A) relight the Foyer and die, B) keep the flame and stay alone, C) pass
    /// the wick on. Short lines only (G16: no story beyond what the PDF says until an outline is approved).
    /// </summary>
    public sealed class EndingView : MonoBehaviour
    {
        public TextMeshProUGUI title;
        public TextMeshProUGUI body;
        public TextMeshProUGUI tally;
        public Image art;
        public Sprite artA, artB, artC;
        public Color tintA = new Color(1f, 0.85f, 0.55f), tintB = new Color(0.93f, 0.9f, 0.83f), tintC = new Color(0.93f, 0.9f, 0.83f);
        public float continueAfter = 2f;

        public EndingId Shown { get; private set; }
        float shownAt;
        bool offered;

        void Start()
        {
            var session = GameSession.Instance;
            Shown = session != null && session.LastEnding != EndingId.None ? session.LastEnding : EndingId.A;
            string k = Shown.ToString().ToLowerInvariant();
            if (title != null) title.text = Loc.Get($"ending.{k}.title");
            if (body != null) body.text = Loc.Get($"ending.{k}.text");
            if (art != null)
            {
                art.sprite = Shown == EndingId.A ? artA : Shown == EndingId.B ? artB : artC;
                art.color = Shown == EndingId.A ? tintA : Shown == EndingId.B ? tintB : tintC;
                art.enabled = art.sprite != null;
                art.preserveAspect = true;
            }
            if (tally != null && session != null)
            {
                int n = 0;
                for (int i = 0; i < 3; i++)
                    if (session.Save.HasEnding((EndingId)i))
                        n++;
                tally.text = string.Format(Loc.Get("ending.unlocked"), n);
            }
            shownAt = Time.unscaledTime;
            Events.Music.Raise(MusicId.Ending);
        }

        void Update()
        {
            if (offered || Time.unscaledTime - shownAt < continueAfter || UiKit.Instance == null)
                return;
            offered = true;
            var button = UiKit.Instance.Button(transform, "Continue", Loc.Get("ending.continue"), new Vector2(0f, -110f), new Vector2(160f, 16f), BackToMenu);
            UiKit.Select(button);
        }

        public void BackToMenu()
        {
            var loader = SceneLoader.Instance;
            if (loader != null)
                loader.Load(SceneNames.MainMenu);
        }
    }
}
