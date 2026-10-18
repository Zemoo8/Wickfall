using UnityEngine;

namespace Wickfall.Presentation
{
    /// <summary>Switches its GameObject off when its feature is hidden in the active phase.</summary>
    [DefaultExecutionOrder(-900)]
    public sealed class PresentationGate : MonoBehaviour
    {
        // Stored as a raw long: Unity cannot serialize a 64-bit enum field.
        [SerializeField] long featureBits;

        public Feature feature
        {
            get => (Feature)featureBits;
            set => featureBits = (long)value;
        }

        void Awake()
        {
            if (feature != Feature.None && !PresentationMode.IsEnabled(feature))
                gameObject.SetActive(false);
        }
    }
}
