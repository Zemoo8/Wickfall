using UnityEngine;
using UnityEngine.InputSystem;

namespace Wickfall
{
    public enum InputDeviceKind { KeyboardMouse, Gamepad }

    /// <summary>
    /// Every control of the game (G26 defaults), built in code so the bindings are easy to read and
    /// rebind. Keyboard + mouse and gamepad (Xbox and PlayStation layouts) work at the same time;
    /// <see cref="LastDevice"/> tells the on-screen prompts which glyphs to show.
    /// </summary>
    public sealed class GameInput
    {
        static GameInput instance;

        public static GameInput Instance
        {
            get
            {
                if (instance == null)
                    instance = new GameInput();
                return instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance?.Map.Disable();
            instance = null;
        }

        public readonly InputActionMap Map;
        public readonly InputAction Move;
        public readonly InputAction Attack;
        public readonly InputAction Art;
        public readonly InputAction Dodge;
        public readonly InputAction Trim;
        public readonly InputAction Wheel;
        public readonly InputAction NextFlame;
        public readonly InputAction PrevFlame;
        public readonly InputAction Scroll;
        public readonly InputAction Interact;
        public readonly InputAction Pause;
        public readonly InputAction DevOverlay;
        public readonly InputAction Point;
        public readonly InputAction[] SelectSlot = new InputAction[WickfallCounts.Flames];

        public InputDeviceKind LastDevice { get; private set; } = InputDeviceKind.KeyboardMouse;

        GameInput()
        {
            Map = new InputActionMap("Gameplay");

            Move = Map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            Move.AddBinding("<Gamepad>/leftStick");
            Move.AddBinding("<Gamepad>/dpad");

            Attack = Button("Attack", "<Keyboard>/j", "<Mouse>/leftButton", "<Gamepad>/buttonWest");
            Art = Button("Art", "<Keyboard>/k", "<Mouse>/rightButton", "<Gamepad>/buttonNorth");
            Dodge = Button("Dodge", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            Trim = Button("Trim", "<Keyboard>/leftShift", "<Gamepad>/leftTrigger");
            Wheel = Button("FlameWheel", "<Keyboard>/q", "<Gamepad>/leftShoulder");
            NextFlame = Button("NextFlame", "<Keyboard>/tab", "<Gamepad>/rightShoulder");
            PrevFlame = Button("PrevFlame", "<Keyboard>/r");
            Interact = Button("Interact", "<Keyboard>/e", "<Gamepad>/buttonEast");
            Pause = Button("Pause", "<Keyboard>/escape", "<Gamepad>/start");
            DevOverlay = Button("DevOverlay", "<Keyboard>/backquote");
            for (int i = 0; i < SelectSlot.Length; i++)
                SelectSlot[i] = Button("Slot" + (i + 1), "<Keyboard>/" + (i + 1));

            Scroll = Map.AddAction("Scroll", InputActionType.Value, "<Mouse>/scroll/y");
            Point = Map.AddAction("Point", InputActionType.Value, "<Mouse>/position");

            Map.Enable();
        }

        InputAction Button(string name, params string[] bindings)
        {
            var action = Map.AddAction(name, InputActionType.Button);
            for (int i = 0; i < bindings.Length; i++)
                action.AddBinding(bindings[i]);
            return action;
        }

        /// <summary>Called once per frame by the game services: tracks which device was used last.</summary>
        public void Update()
        {
            var pad = Gamepad.current;
            if (pad != null && (pad.wasUpdatedThisFrame && (pad.leftStick.ReadValue().sqrMagnitude > 0.1f
                    || pad.buttonSouth.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame
                    || pad.buttonNorth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame
                    || pad.startButton.wasPressedThisFrame || pad.dpad.ReadValue().sqrMagnitude > 0.1f)))
                LastDevice = InputDeviceKind.Gamepad;

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if ((keyboard != null && keyboard.anyKey.wasPressedThisFrame)
                || (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)))
                LastDevice = InputDeviceKind.KeyboardMouse;
        }

        /// <summary>True when the last attack came from the mouse, so it aims at the cursor.</summary>
        public bool AttackFromMouse =>
            Attack.activeControl != null && Attack.activeControl.device is Mouse;

        public bool ArtFromMouse =>
            Art.activeControl != null && Art.activeControl.device is Mouse;

        public void SetGameplayEnabled(bool enabled)
        {
            if (enabled) Map.Enable();
            else Map.Disable();
            // Pause, the overlay and the menus always stay usable.
            Pause.Enable();
            DevOverlay.Enable();
        }

        public string SaveOverrides() => Map.SaveBindingOverridesAsJson();

        public void LoadOverrides(string json)
        {
            Map.RemoveAllBindingOverrides();
            if (!string.IsNullOrEmpty(json))
                Map.LoadBindingOverridesFromJson(json);
        }
    }
}
