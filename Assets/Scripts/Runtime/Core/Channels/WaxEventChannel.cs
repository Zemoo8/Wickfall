using UnityEngine;

namespace Wickfall
{
    [CreateAssetMenu(menuName = "WICKFALL/Events/WaxEventChannel", fileName = "WaxEventChannel")]
    public sealed class WaxEventChannel : EventChannel<WaxChange> { }
}
