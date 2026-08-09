// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleModifier.cs
//
// Author: Mikael Danielsson
// Date Created: 28-05-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace SplineArchitect
{
    public class EHandleModifier
    {
        public static bool CtrlActive(Event e)
        {
#if UNITY_EDITOR_OSX
            return e.command;
#else
            return e.control;
#endif
        }

        public static bool CtrlShiftActive(Event e)
        {
#if UNITY_EDITOR_OSX
            return e.command && e.shift;
#else
            return e.control && e.shift;
#endif
        }

        public static bool ShiftActive(Event e)
        {
            return e.shift;
        }

        public static bool AltActive(Event e)
        {
            return e.alt;
        }

        public static bool DeleteActive(Event e)
        {
            if (e.keyCode == KeyCode.Delete && e.type == EventType.KeyUp)
                return true;

#if UNITY_EDITOR_OSX
            if (e.command && e.keyCode == KeyCode.Backspace && e.type == EventType.KeyDown)
                return true;
#endif

            return false;
        }
    }
}
