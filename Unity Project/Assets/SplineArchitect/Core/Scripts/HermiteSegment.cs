// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: HermiteSegment.cs
//
// Author: Mikael Danielsson
// Date Created: 10-04-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;

using UnityEngine;

namespace SplineArchitect
{
    [Serializable]
    public struct HermiteSegment
    {
        public float timeStart;
        public float timeEnd;
        public float valueStart;
        public float valueEnd;
        public float tangentStart;
        public float tangentEnd;

        public HermiteSegment(float timeStart, float timeEnd, float valueStart, float valueEnd, float tangentStart, float tangentEnd)
        {
            this.timeStart = timeStart;
            this.timeEnd = timeEnd;
            this.valueStart = valueStart;
            this.valueEnd = valueEnd;
            this.tangentStart = tangentStart;
            this.tangentEnd = tangentEnd;
    }
}
}