using UnityEngine;
using UnityEngine.UI;

namespace Wickfall.UI
{
    /// <summary>
    /// Scales a canvas by a whole number so the 480x270 interface stays pixel-crisp:
    /// x4 at 1920x1080, x8 at 4K, x2 on a 1366x768 laptop (with a margin).
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    [ExecuteAlways]
    public sealed class PixelCanvasScaler : MonoBehaviour
    {
        public int referenceWidth = 480;
        public int referenceHeight = 270;
        CanvasScaler scaler;
        int lastW, lastH;

        public static int ScaleFor(int screenW, int screenH, int refW, int refH) =>
            Mathf.Max(1, Mathf.Min(screenW / refW, screenH / refH));

        void OnEnable()
        {
            scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            lastW = lastH = 0;
        }

        void Update()
        {
            if (Screen.width == lastW && Screen.height == lastH)
                return;
            lastW = Screen.width;
            lastH = Screen.height;
            scaler.scaleFactor = ScaleFor(lastW, lastH, referenceWidth, referenceHeight);
        }
    }
}
