using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Wickfall.World
{
    /// <summary>
    /// PDF §2.2 Fonderies de Suie: "fours qui vacillent" — a furnace whose mouth flickers. Its light
    /// gutters and sways, so the shadows the walls cast move across the floor (prompt §9: "flickering
    /// furnaces as moving light and shadow"). It now and then nearly dies, then roars back.
    /// </summary>
    public sealed class Furnace : MonoBehaviour
    {
        public Light2D glow;
        public SpriteRenderer sprite;
        public Sprite[] frames = new Sprite[0];
        public float baseIntensity = 1.1f;
        public float sway = 0.18f;

        Vector3 lightHome;
        float seed;
        float dipUntil = -1f;
        float nextDip;

        void Start()
        {
            seed = Random.value * 100f;
            if (glow != null)
                lightHome = glow.transform.localPosition;
            nextDip = Time.time + Random.Range(2f, 6f);
        }

        void Update()
        {
            float t = Time.time + seed;
            float flicker = 0.75f + 0.25f * Mathf.PerlinNoise(t * 3.1f, 0.3f) + 0.1f * Mathf.Sin(t * 17f);
            if (Time.time >= nextDip)
            {
                dipUntil = Time.time + Random.Range(0.25f, 0.6f);
                nextDip = Time.time + Random.Range(3f, 7f);
            }
            if (Time.time < dipUntil)
                flicker *= 0.25f;
            if (glow != null)
            {
                glow.intensity = baseIntensity * flicker;
                glow.transform.localPosition = lightHome + new Vector3(
                    (Mathf.PerlinNoise(t * 1.3f, 5f) - 0.5f) * 2f * sway,
                    (Mathf.PerlinNoise(t * 1.1f, 9f) - 0.5f) * sway, 0f);
            }
            if (sprite != null && frames.Length > 0)
            {
                int i = Time.time < dipUntil ? frames.Length - 1 : Mathf.FloorToInt(t * 8f) % (frames.Length - 1);
                sprite.sprite = frames[Mathf.Clamp(i, 0, frames.Length - 1)];
            }
        }
    }
}
