using UnityEngine;

namespace Wickfall.Player
{
    /// <summary>
    /// An input source driven by code: the bots and the PlayMode tests use it to play the game
    /// through exactly the same path as the keyboard and gamepad.
    /// </summary>
    public sealed class ScriptedInput : IPlayerInputSource
    {
        public Vector2 Move;
        public bool TrimHeld;
        public bool WheelHeld;
        bool attack, art, dodge, interact;
        int slot = -1;
        int cycle;
        Vector2? aim;

        public void PressAttack(Vector2? aimAt = null) { attack = true; aim = aimAt; }
        public void PressArt(Vector2? aimAt = null) { art = true; aim = aimAt; }
        public void PressDodge() => dodge = true;
        public void PressInteract() => interact = true;
        public void SelectSlot(int index) => slot = index;
        public void Cycle(int direction) => cycle = direction;

        public void Read(ref PlayerIntent intent)
        {
            intent.Move = Move;
            intent.TrimHeld = TrimHeld;
            intent.WheelHeld = WheelHeld;
            intent.AttackPressed = attack;
            intent.ArtPressed = art;
            intent.DodgePressed = dodge;
            intent.InteractPressed = interact;
            intent.SelectSlot = slot;
            intent.CycleFlame = cycle;
            intent.HasAim = aim.HasValue;
            intent.AimPoint = aim ?? Vector2.zero;
            // presses last exactly one frame
            attack = art = dodge = interact = false;
            slot = -1;
            cycle = 0;
            aim = null;
        }
    }
}
