using UnityEngine;

namespace Wickfall
{
    [CreateAssetMenu(menuName = "WICKFALL/Events/RegionEventChannel", fileName = "RegionEventChannel")]
    public sealed class RegionEventChannel : EventChannel<RegionId> { }
}
