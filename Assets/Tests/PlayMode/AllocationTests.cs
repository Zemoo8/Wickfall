using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;
using Wickfall.Meta;

namespace Wickfall.Tests
{
    /// <summary>PDF §4 / prompt: zero allocation in gameplay frames (pools, NonAlloc physics, no LINQ per frame).</summary>
    public class AllocationTests : PlayModeFixture
    {
        protected override string SceneName => SceneNames.RegionBase(RegionId.TallowFields);

        // GC.GetAllocatedBytesForCurrentThread always reads 0 on Unity's Mono; the profiler's own counter works.
        // It also counts the editor and the test runner (about 6 KB a frame here), so the test compares the
        // game playing against that floor, measured with every game script switched off.
        static ProfilerRecorder Recorder() => ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");

        static IEnumerator Average(ProfilerRecorder r, int frames, float[] result, System.Action<int> perFrame = null)
        {
            yield return null;
            yield return null;
            long total = 0;
            for (int i = 0; i < frames; i++)
            {
                perFrame?.Invoke(i);
                yield return null;
                total += r.LastValue;
            }
            result[0] = (float)total / frames;
        }

        [UnityTest]
        public IEnumerator Walking_Fighting_AndHazards_AllocateNothingPerFrame()
        {
            yield return Wait(3f); // warm up: pools, first-use caches, the room groups around the start
            Player.Wax.GodMode = true;
            using (var recorder = Recorder())
            {
                // control: the counter must see a known allocation, or a 0 below would prove nothing
                yield return null;
                var control = new byte[4096];
                long seen = 0;
                for (int i = 0; i < 3; i++)
                {
                    yield return null;
                    seen = System.Math.Max(seen, recorder.LastValue);
                }
                Assert.GreaterOrEqual(seen, 4096, "the profiler counter sees allocations");
                Assert.AreEqual(4096, control.Length);

                // the floor: the editor and the test runner alone
                var scripts = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .Where(b => b.GetType().Namespace != null && b.GetType().Namespace.StartsWith("Wickfall")).ToList();
                foreach (var b in scripts) b.enabled = false;
                var floor = new float[1];
                yield return Average(recorder, 90, floor);
                foreach (var b in scripts) if (b != null) b.enabled = true;
                yield return Wait(1f);

                // the game playing: walking in curves, swinging, enemies chasing, hazards cycling. The same
                // play runs once unmeasured first: pools grow and caches fill once, then never again.
                System.Action<int> play = i =>
                {
                    float t = i / 60f;
                    Input.Move = new Vector2(Mathf.Sin(t * 1.3f), Mathf.Cos(t * 0.9f));
                    if (i % 20 == 0)
                        Input.PressAttack();
                };
                var playing = new float[1];
                yield return Average(recorder, 240, playing, play);
                float warmup = playing[0];
                yield return Average(recorder, 240, playing, play);
                Input.Move = Vector2.zero;
                Player.Wax.GodMode = false;
                float gameplay = playing[0] - floor[0];
                Debug.Log($"[WICKFALL Alloc] floor {floor[0]:0} B/frame, warm-up {warmup:0}, playing {playing[0]:0} B/frame, gameplay {gameplay:0} B/frame");
                Assert.Less(gameplay, 64f, "gameplay frames must not allocate");
            }
        }
    }
}
