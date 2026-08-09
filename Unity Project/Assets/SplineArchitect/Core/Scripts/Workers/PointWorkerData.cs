// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: PointWorkerData.cs
//
// Author: Mikael Danielsson
// Date Created: 14-01-2026
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace SplineArchitect.Workers
{
    public struct PointWorkerData
    {
        public Vector3 point;
        public Vector3 forwardDirection;
        public Vector3 upDirection;
        public Vector3 rightDirection;

        public PointWorkerData(Vector3 point, Vector3 forwardDirection, Vector3 upDirection, Vector3 rightDirection)
        {
            this.point = point;
            this.forwardDirection = forwardDirection;
            this.upDirection = upDirection;
            this.rightDirection = rightDirection;
        }
    }
}
