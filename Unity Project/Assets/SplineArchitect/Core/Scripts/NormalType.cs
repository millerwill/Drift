// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: NormalType.cs
//
// Author: Mikael Danielsson
// Date Created: 28-03-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

namespace SplineArchitect
{
    public enum NormalType : byte
    {
        SPLINE_SPACE,
        CYLINDER_BASED,
        UNITY_CALCULATED,
        UNITY_CALCULATED_SEAMLESS,
        DO_NOT_CALCULATE
    }
}
