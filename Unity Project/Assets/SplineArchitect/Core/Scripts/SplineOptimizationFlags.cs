// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SplineOptimizationFlags.cs
//
// Author: Mikael Danielsson
// Date Created: 08-07-2026
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace SplineArchitect
{
    [System.Flags]
    public enum SplineOptimizationFlags
    {
        NONE = 0,

        DISABLE_NOISE = 1 << 0,
        DISABLE_SCALE = 1 << 1,
        DISABLE_SADDLE_SKEW = 1 << 2,
        DISABLE_Z_ROTATION = 1 << 3,
        DISABLE_SPLINE_PROFILE = 1 << 4,
        DISABLE_LINK_CROSSINGS = 1 << 5,

        DISABLE_ALL = DISABLE_NOISE | DISABLE_SCALE | DISABLE_SADDLE_SKEW | DISABLE_Z_ROTATION | DISABLE_SPLINE_PROFILE | DISABLE_LINK_CROSSINGS,
    }
}
