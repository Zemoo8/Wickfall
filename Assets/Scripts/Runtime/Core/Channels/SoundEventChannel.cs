using UnityEngine;

namespace Wickfall
{
    [CreateAssetMenu(menuName = "WICKFALL/Events/SoundEventChannel", fileName = "SoundEventChannel")]
    public sealed class SoundEventChannel : EventChannel<SoundRequest> { }
}
