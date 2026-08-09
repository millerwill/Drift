// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SearchFlags.cs
//
// Author: Mikael Danielsson
// Date Created: 19-12-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

namespace SplineArchitect
{
    [System.Flags]
    public enum SplineSearchResultFlags
    {
        NONE = 0,
        SEARCH_FORWARD = 1 << 0,
        SEARCH_BACKWARD = 1 << 1,
        SEARCH_CLOSEST_LINK_FORWARD = 1 << 2,
        SEARCH_CLOSEST_LINK_BACKWARD = 1 << 3,
        NEED_SAME_X_POSITION = 1 << 4,
        NEED_SAME_Y_POSITION = 1 << 5,
    }
}
