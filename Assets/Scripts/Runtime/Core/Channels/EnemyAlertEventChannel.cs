using UnityEngine;

namespace Wickfall
{
    [CreateAssetMenu(menuName = "WICKFALL/Events/EnemyAlertEventChannel", fileName = "EnemyAlertEventChannel")]
    public sealed class EnemyAlertEventChannel : EventChannel<EnemyAlertInfo> { }
}
