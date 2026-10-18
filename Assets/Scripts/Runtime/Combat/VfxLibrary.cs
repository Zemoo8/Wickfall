using System;
using UnityEngine;

namespace Wickfall.Combat
{
    /// <summary>Maps each effect id to its prefab (built by WICKFALL > Setup from the VFX sheets).</summary>
    [CreateAssetMenu(menuName = "WICKFALL/VFX Library", fileName = "VfxLibrary")]
    public sealed class VfxLibrary : ScriptableObject
    {
        public const string ResourcePath = "VfxLibrary";

        [Serializable]
        public struct Entry
        {
            public VfxId id;
            public GameObject prefab;
        }

        public Entry[] entries = new Entry[0];
        GameObject[] lookup;

        public GameObject Get(VfxId id)
        {
            if (lookup == null || lookup.Length == 0)
                Build();
            int index = (int)id;
            return index >= 0 && index < lookup.Length ? lookup[index] : null;
        }

        void Build()
        {
            int size = Enum.GetValues(typeof(VfxId)).Length;
            lookup = new GameObject[size];
            for (int i = 0; i < entries.Length; i++)
            {
                int index = (int)entries[i].id;
                if (index >= 0 && index < size)
                    lookup[index] = entries[i].prefab;
            }
        }

        void OnValidate() => lookup = null;
    }
}
