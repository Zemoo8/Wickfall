using UnityEngine;

namespace Wickfall.Player
{
    /// <summary>
    /// Reads <see cref="GameInput"/> into a <see cref="PlayerIntent"/>. Applies the movement mode
    /// (G17: literal 4-way by default, or 8-way input with 4-direction facing) and the trim
    /// hold/toggle option (G26).
    /// </summary>
    public sealed class PlayerInputReader : IPlayerInputSource
    {
        public MovementMode Mode = MovementMode.FourWay;
        public bool TrimToggle;
        public float Deadzone = 0.3f;
        public Camera AimCamera;

        bool trimToggled;
        // 4-way: the axis pressed most recently wins when both are held.
        bool horizontalWasActive;
        bool verticalWasActive;
        bool preferHorizontal;

        public void Read(ref PlayerIntent intent)
        {
            var input = GameInput.Instance;
            Vector2 raw = input.Move.ReadValue<Vector2>();
            intent.Move = Mode == MovementMode.FourWay ? FourWay(raw) : EightWay(raw);

            intent.AttackPressed = input.Attack.WasPressedThisFrame();
            intent.ArtPressed = input.Art.WasPressedThisFrame();
            intent.DodgePressed = input.Dodge.WasPressedThisFrame();
            intent.InteractPressed = input.Interact.WasPressedThisFrame();
            intent.WheelHeld = input.Wheel.IsPressed();
            intent.UseCandlePressed = input.UseCandle.WasPressedThisFrame();
            intent.UseSalvePressed = input.UseSalve.WasPressedThisFrame();

            if (TrimToggle)
            {
                if (input.Trim.WasPressedThisFrame())
                    trimToggled = !trimToggled;
                intent.TrimHeld = trimToggled;
            }
            else
            {
                trimToggled = false;
                intent.TrimHeld = input.Trim.IsPressed();
            }

            intent.SelectSlot = -1;
            for (int i = 0; i < input.SelectSlot.Length; i++)
                if (input.SelectSlot[i].WasPressedThisFrame())
                    intent.SelectSlot = i;

            intent.CycleFlame = 0;
            if (input.NextFlame.WasPressedThisFrame()) intent.CycleFlame = 1;
            else if (input.PrevFlame.WasPressedThisFrame()) intent.CycleFlame = -1;
            float scroll = input.Scroll.ReadValue<float>();
            if (scroll > 0.5f) intent.CycleFlame = -1;
            else if (scroll < -0.5f) intent.CycleFlame = 1;

            intent.HasAim = false;
            if ((intent.AttackPressed && input.AttackFromMouse) || (intent.ArtPressed && input.ArtFromMouse))
            {
                var cam = AimCamera != null ? AimCamera : Camera.main;
                if (cam != null)
                {
                    Vector2 screen = input.Point.ReadValue<Vector2>();
                    intent.AimPoint = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
                    intent.HasAim = true;
                }
            }
        }

        public void ResetToggles() => trimToggled = false;

        Vector2 EightWay(Vector2 raw)
        {
            if (raw.sqrMagnitude < Deadzone * Deadzone)
                return Vector2.zero;
            return raw.sqrMagnitude > 1f ? raw.normalized : raw;
        }

        Vector2 FourWay(Vector2 raw)
        {
            bool h = Mathf.Abs(raw.x) > Deadzone;
            bool v = Mathf.Abs(raw.y) > Deadzone;
            if (h && !horizontalWasActive) preferHorizontal = true;
            if (v && !verticalWasActive) preferHorizontal = false;
            horizontalWasActive = h;
            verticalWasActive = v;

            if (h && v)
            {
                // Same frame or stick input: the larger axis wins, otherwise the newest key.
                if (Mathf.Abs(Mathf.Abs(raw.x) - Mathf.Abs(raw.y)) > 0.25f)
                    preferHorizontal = Mathf.Abs(raw.x) > Mathf.Abs(raw.y);
                return preferHorizontal ? new Vector2(Mathf.Sign(raw.x), 0f) : new Vector2(0f, Mathf.Sign(raw.y));
            }
            if (h) return new Vector2(Mathf.Sign(raw.x), 0f);
            if (v) return new Vector2(0f, Mathf.Sign(raw.y));
            return Vector2.zero;
        }
    }
}
