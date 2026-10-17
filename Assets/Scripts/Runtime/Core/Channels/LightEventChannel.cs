using UnityEngine;

namespace Wickfall
{
    [CreateAssetMenu(menuName = "WICKFALL/Events/LightEventChannel", fileName = "LightEventChannel")]
    public sealed class LightEventChannel : EventChannel<LightState> { }
}
