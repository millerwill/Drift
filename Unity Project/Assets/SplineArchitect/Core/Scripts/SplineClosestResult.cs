// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SplineClosestResult.cs
//
// Author: Mikael Danielsson
// Date Created: 30-12-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;

using UnityEngine;

namespace SplineArchitect
{
    public struct SplineClosestResult
    {
        public Vector3 pointA;
        public Vector3 pointB;
        public double timeA;
        public double timeB;

        public SplineClosestResult(
            Vector3 pointA,
            Vector3 pointB,
            double timeA,
            double timeB)
        {
            this.pointA = pointA;
            this.pointB = pointB;
            this.timeA = timeA;
            this.timeB = timeB;
        }
    }
}