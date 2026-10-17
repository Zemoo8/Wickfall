using UnityEngine;

namespace Wickfall
{
    [CreateAssetMenu(menuName = "WICKFALL/Events/FeedbackEventChannel", fileName = "FeedbackEventChannel")]
    public sealed class FeedbackEventChannel : EventChannel<FeedbackRequest> { }
}
