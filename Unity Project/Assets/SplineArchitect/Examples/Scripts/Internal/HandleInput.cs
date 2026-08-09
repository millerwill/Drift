using UnityEngine;

namespace SplineArchitect.Examples
{
    using UnityEngine;

    public static class HandleInput
    {
        public static bool IsMouseLeftDown()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            UnityEngine.InputSystem.Mouse mouse = UnityEngine.InputSystem.Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        public static bool IsMouseLeftUp()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            UnityEngine.InputSystem.Mouse mouse = UnityEngine.InputSystem.Mouse.current;
            return mouse != null && mouse.leftButton.wasReleasedThisFrame;
#else
            return Input.GetMouseButtonUp(0);
#endif
        }

        public static bool IsEscapeKeyDown()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;
            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        public static bool IsPageDownKeyDown()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if (Application.isMobilePlatform)
            {
                if (UnityEngine.InputSystem.Pointer.current != null &&
                    UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame)
                {
                    Vector2 pos = UnityEngine.InputSystem.Pointer.current.position.ReadValue();
                    float cornerWidth = Screen.width * 0.2f;
                    float cornerHeight = Screen.height * 0.2f;

                    if (pos.x < cornerWidth &&
                        pos.y < cornerHeight)
                    {
                        return true;
                    }
                }

                return false;
            }
            else
            {
                UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;
                return keyboard != null && keyboard.pageDownKey.wasPressedThisFrame;
            }
#else
            return Input.GetKeyDown(KeyCode.PageDown);
#endif
        }

        public static bool IsPageUpKeyDown()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if (Application.isMobilePlatform)
            {
                if (UnityEngine.InputSystem.Pointer.current != null &&
                    UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame)
                {
                    Vector2 pos = UnityEngine.InputSystem.Pointer.current.position.ReadValue();
                    float cornerWidth = Screen.width * 0.2f;
                    float cornerHeight = Screen.height * 0.2f;

                    if (pos.x > Screen.width - cornerWidth &&
                        pos.y < cornerHeight)
                    {
                        return true;
                    }
                }

                return false;
            }
            else
            {
                UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;
                return keyboard != null && keyboard.pageUpKey.wasPressedThisFrame;
            }
#else
            return Input.GetKeyDown(KeyCode.PageUp);
#endif
        }

        public static bool IsHomeKeyDown()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if (Application.isMobilePlatform)
            {
                if (UnityEngine.InputSystem.Pointer.current != null && 
                    UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame)
                {
                    Vector2 pos = UnityEngine.InputSystem.Pointer.current.position.ReadValue();
                    float cornerWidth = Screen.width * 0.2f;
                    float cornerHeight = Screen.height * 0.2f;

                    if (pos.x > Screen.width - cornerWidth &&
                        pos.y > Screen.height - cornerHeight)
                    {
                        return true;
                    }
                }

                return false;
            }
            else
            {
                UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;
                return keyboard != null && keyboard.homeKey.wasPressedThisFrame;
            }
#else
            return Input.GetKeyDown(KeyCode.Home);
#endif
        }

        public static bool IsAnyCtrlKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;
            return keyboard != null && (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed);
#else
            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
#endif
        }

        public static bool IsAnyCommandKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;
            return keyboard != null && keyboard.kKey.isPressed;
#else
            return Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
#endif
        }

        public static Vector2 GetMousePosition()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            UnityEngine.InputSystem.Mouse mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null)
                return mouse.position.ReadValue();
            return Vector2.zero;
#else
            return Input.mousePosition;
#endif
        }

        public static bool IsMouseOverRect(RectTransform rectTransform, Camera camera = null)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, GetMousePosition(), camera);
        }
    }
}
