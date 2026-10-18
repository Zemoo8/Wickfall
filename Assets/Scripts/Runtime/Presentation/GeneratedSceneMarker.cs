using UnityEngine;

namespace Wickfall.Presentation
{
    /// <summary>
    /// Marks a scene as built by WICKFALL > Setup. Setup only rebuilds scenes that carry it, so a
    /// scene someone edited by hand (and removed the marker from) is never overwritten.
    /// </summary>
    public sealed class GeneratedSceneMarker : MonoBehaviour
    {
        [TextArea] public string note = "Built by WICKFALL > Setup. Delete this object to keep manual edits.";
    }
}
