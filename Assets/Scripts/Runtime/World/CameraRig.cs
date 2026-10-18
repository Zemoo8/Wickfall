using UnityEngine;
using UnityEngine.Rendering.Universal;
using Wickfall.Meta;

namespace Wickfall.World
{
    /// <summary>
    /// Follows the player with a little look-ahead, adds screen shake, and drives the Pixel Perfect
    /// Camera (480x270 reference, 16 px per unit). Display option: pixel-perfect (letterbox) or
    /// fill the screen (prompt §10 "Rendering (PC)").
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class CameraRig : MonoBehaviour
    {
        public const int ReferenceWidth = 480;
        public const int ReferenceHeight = 270;

        public Transform target;
        public Camera cam;
        public PixelPerfectCamera pixelPerfect;
        public float smoothTime = 0.12f;
        public float lookAhead = 0.9f;
        public Vector2 offset = new Vector2(0f, 0.8f);

        Vector3 velocity;
        Vector2 lookOffset;
        float shakeStrength;
        float shakeUntil;
        float shakeDuration;
        Vector2 focusOverride;
        float focusWeight;

        public static CameraRig Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            if (cam == null) cam = GetComponent<Camera>();
            if (pixelPerfect == null) pixelPerfect = GetComponent<PixelPerfectCamera>();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void OnEnable()
        {
            Events.Feedback.Register(OnFeedback);
            Events.SettingsChanged.Register(ApplySettings);
        }

        void OnDisable()
        {
            Events.Feedback.Unregister(OnFeedback);
            Events.SettingsChanged.Unregister(ApplySettings);
        }

        void Start() => ApplySettings();

        void ApplySettings()
        {
            if (pixelPerfect == null)
                return;
            var settings = GameServices.Instance != null ? GameServices.Instance.Settings : null;
            bool perfect = settings == null || settings.pixelPerfect;
            pixelPerfect.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            pixelPerfect.cropFrame = perfect ? PixelPerfectCamera.CropFrame.Windowbox : PixelPerfectCamera.CropFrame.StretchFill;
        }

        void OnFeedback(FeedbackRequest request)
        {
            if (request.Kind != FeedbackKind.Shake)
                return;
            float scale = GameServices.Instance != null ? GameServices.Instance.Settings.screenShake : 1f;
            float strength = request.Strength * scale;
            if (strength <= 0f)
                return;
            if (Time.unscaledTime > shakeUntil || strength > shakeStrength)
            {
                shakeStrength = strength;
                shakeDuration = Mathf.Max(0.01f, request.Duration);
                shakeUntil = Time.unscaledTime + shakeDuration;
            }
        }

        /// <summary>Pulls the camera toward a point (the boss arena, a vista) with a weight 0..1.</summary>
        public void SetFocus(Vector2 point, float weight)
        {
            focusOverride = point;
            focusWeight = Mathf.Clamp01(weight);
        }

        public void SnapToTarget()
        {
            if (target == null)
                return;
            Vector3 p = (Vector2)target.position + offset;
            transform.position = new Vector3(p.x, p.y, -10f);
            velocity = Vector3.zero;
        }

        void LateUpdate()
        {
            if (target == null || !target.gameObject.activeInHierarchy)
                return;

            Vector2 targetVelocity = target.TryGetComponent(out Rigidbody2D body) ? body.linearVelocity : Vector2.zero;
            Vector2 desiredLook = targetVelocity.sqrMagnitude > 0.5f ? targetVelocity.normalized * lookAhead : Vector2.zero;
            lookOffset = Vector2.Lerp(lookOffset, desiredLook, 1f - Mathf.Exp(-3f * Time.deltaTime));

            Vector2 goal = (Vector2)target.position + offset + lookOffset;
            if (focusWeight > 0f)
                goal = Vector2.Lerp(goal, focusOverride, focusWeight);
            Vector3 smoothed = Vector3.SmoothDamp(transform.position, new Vector3(goal.x, goal.y, -10f), ref velocity, smoothTime);

            Vector2 shake = Vector2.zero;
            if (Time.unscaledTime < shakeUntil)
            {
                float t = (shakeUntil - Time.unscaledTime) / shakeDuration;
                float amount = shakeStrength * t * t;
                shake = new Vector2(Mathf.PerlinNoise(Time.unscaledTime * 40f, 0.3f) - 0.5f,
                    Mathf.PerlinNoise(0.7f, Time.unscaledTime * 40f) - 0.5f) * (2f * amount);
            }
            transform.position = smoothed + (Vector3)shake;
        }
    }
}
