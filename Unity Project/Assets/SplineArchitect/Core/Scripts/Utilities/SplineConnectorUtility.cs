// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SplineConnectorUtility.cs
//
// Author: Mikael Danielsson
// Date Created: 26-05-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEngine;

namespace SplineArchitect.Utility
{
    public class SplineConnectorUtility
    {
        /// <summary>
        /// Gets the closest spline connector to a given world point.
        /// Get all registerer spline connectors in the HandleRegsitry class.
        /// </summary>
        public static SplineConnector GetClosest(Vector3 worldPoint, 
                                                 HashSet<SplineConnector> splineConnectors)
        {
            float dCheck = 999999;
            SplineConnector closest = null;

            foreach (SplineConnector sc in splineConnectors)
            {
                if(sc == null) 
                    continue;

                float distance = Vector3.Distance(worldPoint, sc.transform.position);

                if (distance < dCheck)
                {
                    dCheck = distance;
                    closest = sc;
                }
            }

            return closest;
        }

        internal static SplineConnector GetFirstConnectorAtPoint(Vector3 worldPoint, HashSet<SplineConnector> splineConnectors, float epsilon = 0.001f)
        {
            foreach (SplineConnector sc in splineConnectors)
            {
                if (sc == null)
                    continue;

                float d = Vector3.Distance(sc.transform.position, worldPoint);

                if (GeneralUtility.IsZero(d, epsilon))
                {
                    return sc;
                }
            }

            return null;
        }
    }
}
