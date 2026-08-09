// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SplineSearchResult.cs
//
// Author: Mikael Danielsson
// Date Created: 09-03-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

namespace SplineArchitect
{
    public struct SplineSearchResult
    {
        public float distanceFromLink;
        public float distanceFromTargetToLink;
        public float distanceFromSplinePoint;
        public SplineObject target;
        public bool isOnLinkedSpline;
        public bool isForwardAlongSpline;

        public SplineSearchResult(float distanceFromLink, float distanceFromTargetToLink, float distanceFromSplinePoint, SplineObject target, bool isOnLinkedSpline, bool isForwardAlongSpline)
        {
            this.distanceFromLink = distanceFromLink;
            this.distanceFromTargetToLink = distanceFromTargetToLink;
            this.distanceFromSplinePoint = distanceFromSplinePoint;
            this.target = target;
            this.isOnLinkedSpline = isOnLinkedSpline;
            this.isForwardAlongSpline = isForwardAlongSpline;
        }
    }
}
