using UnityEngine;

namespace Wickfall
{
    [CreateAssetMenu(menuName = "WICKFALL/Events/PlayerActionEventChannel", fileName = "PlayerActionEventChannel")]
    public sealed class PlayerActionEventChannel : EventChannel<PlayerActionInfo> { }
}
