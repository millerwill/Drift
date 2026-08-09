// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SnapSettings.cs
//
// Author: Mikael Danielsson
// Date Created: 02-02-2026
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;

using UnityEngine;

namespace SplineArchitect
{
    [Serializable]
    public struct SnapSettings
    {
        public float startSnapDistance;
        public float endSnapDistance;
        public float startSnapOffset;
        public float endSnapOffset;
        public float snapTargetPoint;
        public SnapMode snapMode;

        public SnapSettings(SnapMode snapMode)
        {
            startSnapDistance = 1;
            endSnapDistance = 1;
            startSnapOffset = 0;
            endSnapOffset = 0;
            snapTargetPoint = 0;
            this.snapMode = snapMode;
        }
    }
}