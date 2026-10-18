using UnityEngine;
using UnityEngine.UI;

namespace Wickfall.UI
{
    /// <summary>
    /// PDF §3 HUD: "wax bar drawn as a candle that melts down, 8 fill states", with the live flame on
    /// top in the active flame's colour. It listens to the WaxChanged, FlameSwitched and TrimChanged
    /// channels only (PDF §4: "HUD ... écoutent les canaux d'événements").
    /// </summary>
    public sealed class HudCandle : MonoBehaviour
    {
        public HudSprites sprites;
        public Image candle;
        public Image flame;
        public Image currentIcon;
        public float flameFps = 12f;
        public int pixelsPerUnit = 1; // canvas units per sprite pixel

        float normalized = 1f;
        FlameId flameId;
        bool trimmed;
        bool extinguished;
        float time;
        int state = -1;
        float lowPulse;

        void OnEnable()
        {
            Events.WaxChanged.Register(OnWax);
            Events.FlameSwitched.Register(OnFlame);
            Events.TrimChanged.Register(OnTrim);
            if (Events.WaxChanged.HasLast) OnWax(Events.WaxChanged.Last);
            if (Events.FlameSwitched.HasLast) OnFlame(Events.FlameSwitched.Last);
        }

        void OnDisable()
        {
            Events.WaxChanged.Unregister(OnWax);
            Events.FlameSwitched.Unregister(OnFlame);
            Events.TrimChanged.Unregister(OnTrim);
        }

        void OnWax(WaxChange change)
        {
            normalized = change.Max > 0f ? change.Current / change.Max : 0f;
            extinguished = change.Current <= 0f;
            if (change.Delta < -1f)
                lowPulse = 1f; // a hit makes the candle shudder
        }

        void OnFlame(FlameSwitch change)
        {
            flameId = change.To;
            if (currentIcon != null && sprites != null)
                currentIcon.sprite = sprites.flameIcons[(int)flameId];
        }

        void OnTrim(bool value) => trimmed = value;

        /// <summary>0 = full ... 7 = a short stub (8 states).</summary>
        public static int StateFor(float normalized) => Mathf.Clamp(Mathf.FloorToInt((1f - Mathf.Clamp01(normalized)) * 8f), 0, 7);

        void Update()
        {
            if (sprites == null || candle == null)
                return;
            int s = StateFor(normalized);
            if (s != state)
            {
                state = s;
                candle.sprite = sprites.candleStates[s];
                candle.SetNativeSize();
            }

            lowPulse = Mathf.MoveTowards(lowPulse, 0f, Time.unscaledDeltaTime * 4f);
            float shudder = lowPulse > 0f ? Mathf.Round(Mathf.Sin(Time.unscaledTime * 60f) * lowPulse) : 0f;
            candle.rectTransform.anchoredPosition = new Vector2(shudder, 0f);

            if (flame != null)
            {
                var frames = sprites.Flame(flameId);
                flame.enabled = !extinguished && frames != null && frames.Length > 0;
                if (flame.enabled)
                {
                    time += Time.unscaledDeltaTime;
                    flame.sprite = frames[Mathf.FloorToInt(time * flameFps) % frames.Length];
                    flame.SetNativeSize();
                    float top = sprites.candleTops != null && s < sprites.candleTops.Length ? sprites.candleTops[s] : 60f;
                    flame.rectTransform.anchoredPosition = candle.rectTransform.anchoredPosition + new Vector2(0f, top * pixelsPerUnit);
                    float size = trimmed ? 0.5f : Mathf.Lerp(0.7f, 1f, normalized);
                    flame.rectTransform.localScale = new Vector3(Mathf.Round(size * 4f) / 4f, Mathf.Round(size * 4f) / 4f, 1f);
                }
            }
        }
    }
}
