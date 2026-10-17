using UnityEngine;

namespace Wickfall
{
    [CreateAssetMenu(menuName = "WICKFALL/Events/StatusEventChannel", fileName = "StatusEventChannel")]
    public sealed class StatusEventChannel : EventChannel<StatusInfo> { }
}
