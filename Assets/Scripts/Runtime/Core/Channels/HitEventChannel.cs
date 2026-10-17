using UnityEngine;

namespace Wickfall
{
    [CreateAssetMenu(menuName = "WICKFALL/Events/HitEventChannel", fileName = "HitEventChannel")]
    public sealed class HitEventChannel : EventChannel<HitReport> { }
}
