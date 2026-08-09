// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EasingUtility.cs
//
// Author: Mikael Danielsson
// Date Created: 10-03-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace SplineArchitect.Utility
{
    public class EasingUtility
    {
        public static float EvaluateEasing(float value, Easing easing)
        {
            if(easing == Easing.EASE_IN_SINE)
                return 1 - Mathf.Cos(value * Mathf.PI / 2);
            else if (easing == Easing.EASE_IN_OUT_SINE)
                return -(Mathf.Cos(Mathf.PI * value) - 1) / 2;
            else if (easing == Easing.EASE_IN_QUBIC)
                return value * value * value;
            else if (easing == Easing.EASE_IN_OUT_CUBIC)
                return value < 0.5 ? 4 * value * value * value : 1 - Mathf.Pow(-2 * value + 2, 3) / 2;
            else if (easing == Easing.EASE_IN_QUINT)
                return value * value;
            else if (easing == Easing.EASE_IN_OUT_QUINT)
                return value < 0.5 ? 16 * value * value * value * value * value : 1 - Mathf.Pow(-2 * value + 2, 5) / 2;
            else if (easing == Easing.EASE_OUT_QUBIC)
                return 1 - Mathf.Pow(1 - value, 3);
            else if (easing == Easing.EASE_OUT_SINE)
                return Mathf.Sin(value * Mathf.PI / 2);
            else if (easing == Easing.EASE_OUT_CIRC)
                return Mathf.Sqrt(1 - Mathf.Pow(value - 1, 2));
            else if (easing == Easing.EASE_OUT_QUINT)
                return 1 - Mathf.Pow(1 - value, 5);

            return value;
        }
    }
}
