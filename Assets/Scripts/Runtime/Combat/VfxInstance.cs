using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Wickfall.Combat
{
    /// <summary>
    /// A pooled flipbook effect: plays its frames once (or loops for a lifetime), optionally with a
    /// short 2D light flash, then returns to the pool. Tinting recolors the effect for each flame.
    /// </summary>
    public sealed class VfxInstance : MonoBehaviour, IPoolable
    {
        public SpriteRenderer sprite;
        public Sprite[] frames = new Sprite[0];
        public float fps = 16f;
        public float lifetime = -1f; // -1: frames.Length / fps
        public bool loop;
        public Light2D flashLight;
        public float flashIntensity = 1.2f;
        public Vector2 drift;
        public bool fadeOut = true;

        float age;
        float duration;
        Color baseColor = Color.white;
        PooledObject pooled;

        public void SetTint(Color tint)
        {
            baseColor = tint;
            if (sprite != null)
                sprite.color = tint;
            if (flashLight != null)
                flashLight.color = tint;
        }

        public void OnSpawned()
        {
            if (pooled == null)
                pooled = GetComponent<PooledObject>();
            age = 0f;
            baseColor = Color.white;
            if (sprite != null)
            {
                sprite.color = Color.white;
                if (frames.Length > 0)
                    sprite.sprite = frames[0];
            }
            duration = lifetime > 0f ? lifetime : Mathf.Max(0.05f, frames.Length / Mathf.Max(1f, fps));
            if (flashLight != null)
            {
                flashLight.color = Color.white;
                flashLight.intensity = flashIntensity;
            }
        }

        public void OnDespawned() { }

        void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / duration);
            if (sprite != null && frames.Length > 0)
            {
                int frame = Mathf.FloorToInt(age * fps);
                frame = loop ? frame % frames.Length : Mathf.Min(frame, frames.Length - 1);
                sprite.sprite = frames[frame];
                if (fadeOut && loop)
                {
                    var c = baseColor;
                    c.a *= 1f - t * t;
                    sprite.color = c;
                }
            }
            if (flashLight != null)
                flashLight.intensity = flashIntensity * (1f - t);
            if (drift.sqrMagnitude > 0f)
                transform.position += (Vector3)(drift * Time.deltaTime);

            if (age >= duration)
            {
                if (pooled != null) pooled.Release();
                else gameObject.SetActive(false);
            }
        }
    }
}
