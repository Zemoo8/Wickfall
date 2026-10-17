using UnityEngine;

namespace Wickfall
{
    [CreateAssetMenu(menuName = "WICKFALL/Events/FlareEventChannel", fileName = "FlareEventChannel")]
    public sealed class FlareEventChannel : EventChannel<FlareInfo> { }
}
