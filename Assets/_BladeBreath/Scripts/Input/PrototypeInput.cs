using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BladeBreath
{
    public static class PrototypeInput
    {
#if UNITY_EDITOR
        private static bool _useEditorValidationInput;
        private static Vector2 _editorMove;
        private static bool _editorAttackPressed;
        private static bool _editorHeavyPressed;
        private static bool _editorSkillPressed;
        private static bool _editorSkill2Pressed;
        private static bool _editorSkill3Pressed;
        private static bool _editorGuardPressed;
        private static bool _editorGuardHeld;
        private static bool _editorDodgePressed;
        private static bool _editorResetPressed;

        public static void SetEditorValidationInput(Vector2 move, bool attackPressed = false,
            bool heavyPressed = false, bool guardPressed = false, bool guardHeld = false,
            bool dodgePressed = false, bool resetPressed = false, bool skillPressed = false,
            bool skill2Pressed = false, bool skill3Pressed = false)
        {
            _useEditorValidationInput = true;
            _editorMove = Vector2.ClampMagnitude(move, 1f);
            _editorAttackPressed = attackPressed;
            _editorHeavyPressed = heavyPressed;
            _editorSkillPressed = skillPressed;
            _editorSkill2Pressed = skill2Pressed;
            _editorSkill3Pressed = skill3Pressed;
            _editorGuardPressed = guardPressed;
            _editorGuardHeld = guardHeld;
            _editorDodgePressed = dodgePressed;
            _editorResetPressed = resetPressed;
        }

        public static void ClearEditorValidationInput()
        {
            _useEditorValidationInput = false;
            _editorMove = Vector2.zero;
            _editorAttackPressed = false;
            _editorHeavyPressed = false;
            _editorSkillPressed = false;
            _editorSkill2Pressed = false;
            _editorSkill3Pressed = false;
            _editorGuardPressed = false;
            _editorGuardHeld = false;
            _editorDodgePressed = false;
            _editorResetPressed = false;
        }

        private static bool Consume(ref bool value)
        {
            bool result = value;
            value = false;
            return result;
        }
#endif

        public static Vector2 Move
        {
            get
            {
#if UNITY_EDITOR
                if (_useEditorValidationInput) return _editorMove;
#endif
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
#if UNITY_EDITOR
                if (_useEditorValidationInput) return Consume(ref _editorAttackPressed);
#endif
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

        public static bool HeavyAttackPressed
        {
            get
            {
#if UNITY_EDITOR
                if (_useEditorValidationInput) return Consume(ref _editorHeavyPressed);
#endif
                bool pressed = false;
#if ENABLE_INPUT_SYSTEM
                pressed |= Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame;
                pressed |= Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                pressed |= UnityEngine.Input.GetKeyDown(KeyCode.L);
#endif
                return pressed;
            }
        }

        public static bool Skill1Pressed
        {
            get
            {
#if UNITY_EDITOR
                if (_useEditorValidationInput) return Consume(ref _editorSkillPressed);
#endif
                bool pressed = false;
#if ENABLE_INPUT_SYSTEM
                pressed |= Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame;
                pressed |= Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                pressed |= UnityEngine.Input.GetKeyDown(KeyCode.H);
#endif
                return pressed;
            }
        }

        // Kept as a compatibility alias for the first M0.3 skill check.
        public static bool SkillPressed => Skill1Pressed;

        public static bool Skill2Pressed
        {
            get
            {
#if UNITY_EDITOR
                if (_useEditorValidationInput) return Consume(ref _editorSkill2Pressed);
#endif
                bool pressed = false;
#if ENABLE_INPUT_SYSTEM
                pressed |= Keyboard.current != null && Keyboard.current.uKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                pressed |= UnityEngine.Input.GetKeyDown(KeyCode.U);
#endif
                return pressed;
            }
        }

        public static bool Skill3Pressed
        {
            get
            {
#if UNITY_EDITOR
                if (_useEditorValidationInput) return Consume(ref _editorSkill3Pressed);
#endif
                bool pressed = false;
#if ENABLE_INPUT_SYSTEM
                pressed |= Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                pressed |= UnityEngine.Input.GetKeyDown(KeyCode.I);
#endif
                return pressed;
            }
        }

        public static bool GuardPressed
        {
            get
            {
#if UNITY_EDITOR
                if (_useEditorValidationInput) return Consume(ref _editorGuardPressed);
#endif
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
#if UNITY_EDITOR
                if (_useEditorValidationInput) return _editorGuardHeld;
#endif
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
#if UNITY_EDITOR
                if (_useEditorValidationInput) return Consume(ref _editorDodgePressed);
#endif
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
#if UNITY_EDITOR
                if (_useEditorValidationInput) return Consume(ref _editorResetPressed);
#endif
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
