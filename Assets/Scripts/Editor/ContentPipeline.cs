using UnityEngine;

namespace Wickfall.EditorTools
{
    /// <summary>
    /// Builds the generated content (art import, prefabs, scenes) in dependency order.
    /// Each step only creates what is missing, except scenes marked as generated, which it rebuilds.
    /// </summary>
    public static class ContentPipeline
    {
        public static void EnsureAll()
        {
            Debug.Log("[WICKFALL Setup] Content pipeline: nothing to build yet.");
        }
    }
}
