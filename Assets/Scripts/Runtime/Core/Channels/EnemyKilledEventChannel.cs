using UnityEngine;

namespace Wickfall
{
    [CreateAssetMenu(menuName = "WICKFALL/Events/EnemyKilledEventChannel", fileName = "EnemyKilledEventChannel")]
    public sealed class EnemyKilledEventChannel : EventChannel<EnemyKilledInfo> { }
}
