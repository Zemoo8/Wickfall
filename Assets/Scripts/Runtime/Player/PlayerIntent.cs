using UnityEngine;

namespace Wickfall.Player
{
    /// <summary>What the player wants this frame. Filled by the keyboard/gamepad reader or by a bot.</summary>
    public struct PlayerIntent
    {
        public Vector2 Move;
        public bool AttackPressed;
        public bool ArtPressed;
        public bool DodgePressed;
        public bool TrimHeld;
        public bool InteractPressed;
        public bool WheelHeld;
        public bool UseCandlePressed;
        public bool UseSalvePressed;
        /// <summary>Loadout slot chosen this frame (0..4), or -1.</summary>
        public int SelectSlot;
        /// <summary>+1 next flame, -1 previous flame, 0 none.</summary>
        public int CycleFlame;
        /// <summary>World point to aim at (mouse), valid when HasAim.</summary>
        public Vector2 AimPoint;
        public bool HasAim;

        public void ClearPresses()
        {
            AttackPressed = ArtPressed = DodgePressed = InteractPressed = UseCandlePressed = UseSalvePressed = false;
            SelectSlot = -1;
            CycleFlame = 0;
            HasAim = false;
        }
    }

    public interface IPlayerInputSource
    {
        void Read(ref PlayerIntent intent);
    }
}
