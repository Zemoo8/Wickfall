using UnityEngine;

namespace Wickfall
{
    [CreateAssetMenu(menuName = "WICKFALL/Events/DodgeEventChannel", fileName = "DodgeEventChannel")]
    public sealed class DodgeEventChannel : EventChannel<DodgeState> { }
}
