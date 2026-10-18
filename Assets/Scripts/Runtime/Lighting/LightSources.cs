using System.Collections.Generic;
using UnityEngine;

namespace Wickfall.Lighting
{
    /// <summary>
    /// Every light that creatures can sense: the player's flame, lit braziers, flares, furnaces.
    /// Les Éteints drift toward the strongest one (PDF §2.2: "dérivent vers la lumière la plus forte").
    /// </summary>
    public static class LightSources
    {
        public struct Source
        {
            public Vector2 Position;
            public float Strength; // roughly the light radius in units
            public int Id;
            public bool IsPlayer;
        }

        static readonly List<Source> sources = new List<Source>(64);
        static readonly List<float> expiries = new List<float>(64);
        static int nextId = 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            sources.Clear();
            expiries.Clear();
            nextId = 1;
        }

        public static int Count => sources.Count;
        public static Source Get(int index) => sources[index];

        /// <summary>Adds or moves a source. Pass the id you got back last time (0 for a new one).</summary>
        public static int Set(int id, Vector2 position, float strength, bool isPlayer = false, float lifetime = -1f)
        {
            float expiry = lifetime > 0f ? Time.time + lifetime : float.PositiveInfinity;
            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i].Id != id)
                    continue;
                sources[i] = new Source { Position = position, Strength = strength, Id = id, IsPlayer = isPlayer };
                expiries[i] = expiry;
                return id;
            }
            int newId = nextId++;
            sources.Add(new Source { Position = position, Strength = strength, Id = newId, IsPlayer = isPlayer });
            expiries.Add(expiry);
            return newId;
        }

        public static void Remove(int id)
        {
            for (int i = sources.Count - 1; i >= 0; i--)
            {
                if (sources[i].Id == id)
                {
                    sources.RemoveAt(i);
                    expiries.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// The most attractive light from a point: strength weighed against distance, within range.
        /// </summary>
        public static bool TryStrongest(Vector2 from, float range, out Source best)
        {
            best = default;
            float bestScore = 0f;
            float now = Time.time;
            for (int i = sources.Count - 1; i >= 0; i--)
            {
                if (expiries[i] < now)
                {
                    sources.RemoveAt(i);
                    expiries.RemoveAt(i);
                    continue;
                }
                var s = sources[i];
                float distance = Vector2.Distance(from, s.Position);
                if (distance > range + s.Strength)
                    continue;
                float score = s.Strength / (1f + distance * 0.35f);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = s;
                }
            }
            return bestScore > 0f;
        }
    }
}
