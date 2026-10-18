using System;
using UnityEngine;
using Wickfall.Presentation;

namespace Wickfall
{
    /// <summary>One flipbook animation: frames, speed, loop.</summary>
    [Serializable]
    public struct SpriteClip
    {
        public Sprite[] frames;
        public float fps;
        public bool loop;

        public bool IsValid => frames != null && frames.Length > 0;
        public float Duration => IsValid ? frames.Length / Mathf.Max(0.01f, fps) : 0f;

        public SpriteClip(Sprite[] frames, float fps, bool loop)
        {
            this.frames = frames;
            this.fps = fps;
            this.loop = loop;
        }
    }

    /// <summary>
    /// Plays <see cref="SpriteClip"/>s on a SpriteRenderer. When the active presentation phase hides
    /// animation (Phase 4: placeholder polish), it shows each clip's first frame only.
    /// </summary>
    public sealed class SpriteAnimator : MonoBehaviour
    {
        public SpriteRenderer target;
        public float speed = 1f;

        Sprite[] frames;
        float fps = 10f;
        bool loop = true;
        float time;
        int frame = -1;
        static bool animationChecked;
        static bool animationEnabled = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => animationChecked = false;

        public bool Finished { get; private set; }
        public int Frame => frame;
        public float NormalizedTime => frames == null || frames.Length == 0 ? 1f : Mathf.Clamp01(time * fps / frames.Length);
        public Sprite[] CurrentFrames => frames;

        void Awake()
        {
            if (target == null)
                target = GetComponent<SpriteRenderer>();
        }

        public void Play(in SpriteClip clip, bool restart = false) => Play(clip.frames, clip.fps, clip.loop, restart);

        public void Play(Sprite[] newFrames, float newFps, bool newLoop, bool restart = false)
        {
            if (newFrames == null || newFrames.Length == 0)
                return;
            if (!restart && ReferenceEquals(newFrames, frames))
            {
                fps = newFps;
                loop = newLoop;
                return;
            }
            frames = newFrames;
            fps = Mathf.Max(0.01f, newFps);
            loop = newLoop;
            time = 0f;
            frame = -1;
            Finished = false;
            Apply(0);
        }

        /// <summary>Shows one frame and stops (used by the showcase and still captures).</summary>
        public void Show(Sprite sprite)
        {
            frames = null;
            if (target != null)
                target.sprite = sprite;
        }

        void Update()
        {
            if (frames == null || frames.Length == 0)
                return;
            if (!animationChecked)
            {
                animationChecked = true;
                animationEnabled = PresentationMode.IsEnabled(Feature.Animation);
            }
            if (!animationEnabled)
            {
                Apply(0);
                return;
            }

            time += Time.deltaTime * speed;
            int index = Mathf.FloorToInt(time * fps);
            if (loop)
            {
                index %= frames.Length;
            }
            else if (index >= frames.Length)
            {
                index = frames.Length - 1;
                Finished = true;
            }
            Apply(index);
        }

        void Apply(int index)
        {
            if (index == frame || target == null)
                return;
            frame = index;
            target.sprite = frames[index];
        }
    }
}
