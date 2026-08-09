// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: HandleRegistry.cs
//
// Author: Mikael Danielsson
// Date Created: 05-09-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

namespace SplineArchitect
{
    public class HandleRegistry
    {
        private static HashSet<Spline> registrySplines = new HashSet<Spline>();
        private static HashSet<SplineConnector> registrySplineConnectors = new HashSet<SplineConnector>();

#if UNITY_EDITOR
        private static HashSet<Spline> registrySplinesPrefabStage = new HashSet<Spline>();
        private static HashSet<SplineConnector> registrySplineConnectorsPrefabStage = new HashSet<SplineConnector>();

        private static HashSet<Spline> registrySplinesThisFrame = new HashSet<Spline>();
        private static HashSet<SplineConnector> registrySplineConnectorsThisFrame = new HashSet<SplineConnector>();
#endif
        /// <summary>
        /// UNSAFE: Returns the live backing HashSet. Do NOT add, remove, or clear elements.
        /// Modifying this collection will break the internal state of the registry.
        /// </summary>
        public static HashSet<Spline> GetSplinesUnsafe()
        {
#if UNITY_EDITOR
            if (EHandlePrefab.IsPrefabStageActive())
                return registrySplinesPrefabStage;
#endif
            return registrySplines;
        }

        /// <summary>
        /// Fills the provided list with all currently registered splines.
        /// The list is not cleared automatically.
        /// </summary>
        public static void GetSplines(List<Spline> result)
        {
            foreach(Spline spline in registrySplines)
            {
                result.Add(spline);
            }
        }

        /// <summary>
        /// UNSAFE: Returns the live backing HashSet. Do NOT add, remove, or clear elements.
        /// Modifying this collection will break the internal state of the registry.
        /// </summary>
        public static HashSet<SplineConnector> GetSplineConnectorsUnsafe()
        {
#if UNITY_EDITOR
            if (EHandlePrefab.IsPrefabStageActive())
                return registrySplineConnectorsPrefabStage;
#endif

            return registrySplineConnectors;
        }

        /// <summary>
        /// Fills the provided list with all currently registered spline connectors.
        /// The list is not cleared automatically.
        /// </summary>
        public static void GetSplineConnectors(List<SplineConnector> result)
        {
            foreach (SplineConnector splineConnector in registrySplineConnectors)
            {
                result.Add(splineConnector);
            }
        }

        /// <summary>
        /// Gets the total length of all registered splines.
        /// </summary>
        public static float GetTotalLengthOfAllSplines()
        {
            float totalLength = 0f;

            foreach (Spline spline in GetSplinesUnsafe())
            {
                totalLength += spline.Length;
            }

            return totalLength;
        }

        internal static void AddSpline(Spline spline)
        {
#if UNITY_EDITOR
            registrySplinesThisFrame.Add(spline);

            if (EHandlePrefab.IsPartOfActivePrefabStage(spline.gameObject))
            {
                registrySplinesPrefabStage.Add(spline);
                return;
            }
#endif

            registrySplines.Add(spline);
        }

        internal static void RemoveSpline(Spline spline)
        {
#if UNITY_EDITOR
            registrySplinesPrefabStage.Remove(spline);
            registrySplinesThisFrame.Remove(spline);
#endif
            registrySplines.Remove(spline);
        }

        internal static bool ContainsSpline(Spline spline)
        {
#if UNITY_EDITOR
            if (registrySplinesPrefabStage.Contains(spline))
                return true;
#endif
            if (registrySplines.Contains(spline))
                return true;

            return false;
        }

        internal static void AddSplineConnector(SplineConnector sc)
        {
#if UNITY_EDITOR
            registrySplineConnectorsThisFrame.Add(sc);

            if (EHandlePrefab.IsPartOfActivePrefabStage(sc.gameObject))
            {
                registrySplineConnectorsPrefabStage.Add(sc);
                return;
            }
#endif

            registrySplineConnectors.Add(sc);
        }

        internal static void RemoveSplineConnector(SplineConnector sc)
        {
#if UNITY_EDITOR
            registrySplineConnectorsPrefabStage.Remove(sc);
            registrySplineConnectorsThisFrame.Remove(sc);
#endif

            registrySplineConnectors.Remove(sc);
        }

#if UNITY_EDITOR
        internal static HashSet<Spline> GetSplinesRegistredThisFrame()
        {
            return registrySplinesThisFrame;
        }

        internal static HashSet<SplineConnector> GetSplineConnectorsRegistredThisFrame()
        {
            return registrySplineConnectorsThisFrame;
        }

        internal static void UpdateGlobal()
        {
            registrySplinesThisFrame.Clear();
            registrySplineConnectorsThisFrame.Clear();
        }

        internal static void DisposeNativeDataOnSplines()
        {
            foreach (Spline spline in registrySplines)
            {
                spline.DisposeCache();
            }

            foreach (Spline spline in registrySplinesPrefabStage)
            {
                spline.DisposeCache();
            }
        }
#endif
    }
}
