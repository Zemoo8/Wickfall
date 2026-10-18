using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Wickfall.UI
{
    /// <summary>A full-screen charcoal fade used between scenes and on death (unscaled time).</summary>
    public sealed class ScreenFader : MonoBehaviour
    {
        public static ScreenFader Instance { get; private set; }
        public Image image;
        public float defaultSeconds = 0.45f;

        public bool IsBlack => image != null && image.color.a >= 0.99f;

        void Awake()
        {
            Instance = this;
            if (image != null)
                image.raycastTarget = false;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public IEnumerator FadeOut(float seconds = -1f) => Fade(1f, seconds < 0f ? defaultSeconds : seconds);
        public IEnumerator FadeIn(float seconds = -1f) => Fade(0f, seconds < 0f ? defaultSeconds : seconds);

        public void SetBlack(bool black)
        {
            if (image == null)
                return;
            var c = image.color;
            c.a = black ? 1f : 0f;
            image.color = c;
            image.enabled = black;
        }

        IEnumerator Fade(float target, float seconds)
        {
            if (image == null)
                yield break;
            image.enabled = true;
            float start = image.color.a;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                var c = image.color;
                c.a = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / seconds));
                image.color = c;
                yield return null;
            }
            var end = image.color;
            end.a = target;
            image.color = end;
            image.enabled = target > 0f;
        }
    }
}
