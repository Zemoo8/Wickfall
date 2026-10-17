using UnityEngine;

namespace Wickfall
{
    [CreateAssetMenu(menuName = "WICKFALL/Events/PromptEventChannel", fileName = "PromptEventChannel")]
    public sealed class PromptEventChannel : EventChannel<InteractPromptInfo> { }
}
