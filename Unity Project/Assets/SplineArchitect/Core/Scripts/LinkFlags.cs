// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: LinkFlags.cs
//
// Author: Mikael Danielsson
// Date Created: 05-12-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace SplineArchitect
{
    [System.Flags]
    public enum LinkFlags
    {
        NONE = 0,
        SKIP_LAST = 1 << 0,
        SKIP_FIRST = 1 << 1,
        SKIP_SELF = 1 << 2
    }
}
