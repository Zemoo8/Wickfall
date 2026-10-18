using UnityEngine;
using UnityEngine.UI;
using Wickfall.Meta;
using Wickfall.Presentation;

namespace Wickfall.UI
{
    /// <summary>
    /// Game feel (prompt §18.7): hit-stop and the screen flash. Shake is handled by the camera rig.
    /// Both respect the accessibility sliders (shake and flash intensity) and Phase 4's "no juice".
    /// </summary>
    public sealed class FeedbackDirector : MonoBehaviour
    {
        public Image flashImage;

        float hitStopUntil;
        float flashAlpha;
        float flashDecay = 8f;
        bool inHitStop;

        void OnEnable() => Events.Feedback.Register(OnFeedback);
        void OnDisable() => Events.Feedback.Unregister(OnFeedback);

        void OnFeedback(FeedbackRequest request)
        {
            if (!PresentationMode.IsEnabled(Feature.CameraFeedback))
                return;
            switch (request.Kind)
            {
                case FeedbackKind.HitStop:
                    if (GameSession.Instance != null && GameSession.Instance.Paused)
                        return;
                    hitStopUntil = Mathf.Max(hitStopUntil, Time.unscaledTime + request.Duration);
                    if (!inHitStop)
                    {
                        inHitStop = true;
                        Time.timeScale = 0.05f;
                    }
                    break;
                case FeedbackKind.Flash:
                    float intensity = GameServices.Instance != null ? GameServices.Instance.Settings.flashIntensity : 1f;
                    flashAlpha = Mathf.Max(flashAlpha, request.Strength * intensity);
                    flashDecay = 1f / Mathf.Max(0.02f, request.Duration);
                    if (flashImage != null)
                    {
                        var c = request.Color.a > 0f ? request.Color : Color.white;
                        c.a = flashAlpha;
                        flashImage.color = c;
                    }
                    break;
            }
        }

        void Update()
        {
            if (inHitStop && Time.unscaledTime >= hitStopUntil)
            {
                inHitStop = false;
                bool paused = GameSession.Instance != null && GameSession.Instance.Paused;
                Time.timeScale = paused ? 0f : 1f;
            }

            if (flashImage != null)
            {
                flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, flashDecay * Time.unscaledDeltaTime * Mathf.Max(0.2f, flashAlpha));
                var c = flashImage.color;
                c.a = flashAlpha;
                flashImage.color = c;
                flashImage.enabled = flashAlpha > 0.003f;
            }
        }
    }
}
