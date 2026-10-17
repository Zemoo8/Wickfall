using UnityEngine;

namespace Wickfall
{
    public enum MovementMode
    {
        FourWay = 0,        // G17 (a): the literal PDF "déplacement 4 directions" (default)
        EightWayInput = 1,  // G17 (b): 8-way input, 4-direction facing
    }

    /// <summary>Default control behaviour (G17, G26). The player's own choices live in the settings file.</summary>
    [CreateAssetMenu(menuName = "WICKFALL/Config/Controls", fileName = "Controls")]
    public sealed class ControlsSO : ScriptableObject
    {
        public MovementMode defaultMovementMode = MovementMode.FourWay;
        [Tooltip("G26: offer toggle instead of hold for trimming the wick. PDF default is hold.")]
        public bool defaultTrimToggle;
        [Tooltip("An action pressed this early is kept and played when the current one ends.")]
        public float inputBufferSeconds = 0.15f;
        [Range(0f, 0.9f)] public float stickDeadzone = 0.3f;
    }
}
