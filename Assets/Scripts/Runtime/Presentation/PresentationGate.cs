using UnityEngine;

namespace Wickfall.Presentation
{
    /// <summary>Switches its GameObject off when its feature is hidden in the active phase.</summary>
    [DefaultExecutionOrder(-900)]
    public sealed class PresentationGate : MonoBehaviour
    {
        public Feature feature;

        void Awake()
        {
            if (feature != Feature.None && !PresentationMode.IsEnabled(feature))
                gameObject.SetActive(false);
        }
    }
}
