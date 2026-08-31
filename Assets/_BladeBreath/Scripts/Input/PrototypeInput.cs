using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BladeBreath
{
    public static class PrototypeInput
    {
        public static Vector2 Move
        {
            get
            {
                Vector2 value = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) value.x -= 1f;
                    if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) value.x += 1f;
                    if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) value.y -= 1f;
                    if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) value.y += 1f;
                }

                Gamepad gamepad = Gamepad.current;
                if (gamepad != null)
                {
                    Vector2 stick = gamepad.leftStick.ReadValue();
                    if (stick.sqrMagnitude > value.sqrMagnitude)
                    {
                        value = stick;
                    }
                }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
                Vector2 legacy = new Vector2(
                    UnityEngine.Input.GetAxisRaw("Horizontal"),
                    UnityEngine.Input.GetAxisRaw("Vertical"));

                if (legacy.sqrMagnitude > value.sqrMagnitude)
                {
                    value = legacy;
                }
#endif
                return Vector2.ClampMagnitude(value, 1f);
            }
        }

        public static bool AttackPressed
        {
            get
            {
                bool pressed = false;
#if ENABLE_INPUT_SYSTEM
                pressed |= Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame;
                pressed |= Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
                pressed |= Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                pressed |= UnityEngine.Input.GetKeyDown(KeyCode.J);
                pressed |= UnityEngine.Input.GetMouseButtonDown(0);
#endif
                return pressed;
            }
        }

        public static bool GuardPressed
        {
            get
            {
                bool pressed = false;
#if ENABLE_INPUT_SYSTEM
                pressed |= Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame;
                pressed |= Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
                pressed |= Gamepad.current != null && Gamepad.current.leftShoulder.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                pressed |= UnityEngine.Input.GetKeyDown(KeyCode.K);
                pressed |= UnityEngine.Input.GetMouseButtonDown(1);
#endif
                return pressed;
            }
        }

        public static bool GuardHeld
        {
            get
            {
                bool held = false;
#if ENABLE_INPUT_SYSTEM
                held |= Keyboard.current != null && Keyboard.current.kKey.isPressed;
                held |= Mouse.current != null && Mouse.current.rightButton.isPressed;
                held |= Gamepad.current != null && Gamepad.current.leftShoulder.isPressed;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                held |= UnityEngine.Input.GetKey(KeyCode.K);
                held |= UnityEngine.Input.GetMouseButton(1);
#endif
                return held;
            }
        }

        public static bool DodgePressed
        {
            get
            {
                bool pressed = false;
#if ENABLE_INPUT_SYSTEM
                pressed |= Keyboard.current != null &&
                           (Keyboard.current.spaceKey.wasPressedThisFrame ||
                            Keyboard.current.leftShiftKey.wasPressedThisFrame ||
                            Keyboard.current.rightShiftKey.wasPressedThisFrame);
                pressed |= Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                pressed |= UnityEngine.Input.GetKeyDown(KeyCode.Space);
                pressed |= UnityEngine.Input.GetKeyDown(KeyCode.LeftShift);
                pressed |= UnityEngine.Input.GetKeyDown(KeyCode.RightShift);
#endif
                return pressed;
            }
        }

        public static bool ResetPressed
        {
            get
            {
                bool pressed = false;
#if ENABLE_INPUT_SYSTEM
                pressed |= Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
                pressed |= Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                pressed |= UnityEngine.Input.GetKeyDown(KeyCode.R);
#endif
                return pressed;
            }
        }
    }
}
