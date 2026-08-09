// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: LineUtility.cs
//
// Author: Mikael Danielsson
// Date Created: 27-05-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace SplineArchitect.Utility
{
    public static class LineUtility
    {
        public static Vector3 GetNearestPointOnLineFromLine(Vector3 line1Point,
                                                            Vector3 line1Direction,
                                                            Vector3 line2Point,
                                                            Vector3 line2Direction,
                                                            float maxLength = 0,
                                                            bool noBackwards = false)
        {
            // Calculate the cross product of line2Direction and the vector between line2Point and line1Point
            Vector3 cross1 = Vector3.Cross(line2Direction, line2Point - line1Point);
            // Calculate the cross product of line1Direction and line2Direction
            Vector3 cross2 = Vector3.Cross(line1Direction, line2Direction);
            // Calculate time
            float time = Vector3.Dot(cross1, cross2) / cross2.sqrMagnitude;

            if (noBackwards && time < 0)
                time = 0;

            if (maxLength > 0 && time > maxLength)
                time = maxLength;

            // Return the point on Line 1 that's closest to Line 2
            return line1Point + time * -line1Direction;
        }

        public static Vector3 GetNearestPointOnLineFromLine(Vector3 line1Point, 
                                                            Vector3 line1Direction, 
                                                            Vector3 line2Point, 
                                                            Vector3 line2Direction, 
                                                            out float time)
        {
            Vector3 cross1 = Vector3.Cross(line2Direction, line2Point - line1Point);
            Vector3 cross2 = Vector3.Cross(line1Direction, line2Direction);
            time = Vector3.Dot(cross1, cross2) / cross2.sqrMagnitude;
            return line1Point + time * -line1Direction;
        }

        public static Vector3 GetNearestPoint(Vector3 startPoint, 
                                              Vector3 direction, 
                                              Vector3 point, 
                                              out float time)
        {
            Vector3 vVector1 = point - startPoint;
            time = Vector3.Dot(direction, vVector1);
            return startPoint + direction * time;
        }

        public static Vector3 GetNearestPoint(Vector3 startPoint, 
                                              Vector3 direction, 
                                              Vector3 point, 
                                              float minTime, 
                                              float maxTime)
        {
            Vector3 vVector1 = point - startPoint;
            float time = Vector3.Dot(direction, vVector1);
            time = Mathf.Clamp(time, minTime, maxTime);

            return startPoint + direction * time;
        }
    }
}
