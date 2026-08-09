// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SplineObjectUtility.cs
//
// Author: Mikael Danielsson
// Date Created: 11-09-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using Unity.Mathematics;
using UnityEngine;

namespace SplineArchitect.Utility
{
    public class SplineObjectUtility
    {
        public static Bounds GetCombinedBounds(SplineObject so)
        {
            Bounds bounds = new Bounds();
            bool initalized = false;

            for(int i = 0; i < so.MeshContainerCount; i++)
            {
                MeshContainer mc = so.GetMeshContainerAtIndex(i);
                Mesh sharedMesh = mc.GetInstanceMesh();

                if (sharedMesh == null)
                    continue;

                Bounds localBounds = sharedMesh.bounds;
                Vector3 worldCenter = so.transform.TransformPoint(localBounds.center);
                Vector3 worldSize = Vector3.Scale(localBounds.size, so.transform.lossyScale);
                Bounds worldBounds = new Bounds(worldCenter, worldSize);

                if (!initalized)
                {
                    initalized = true;
                    bounds = worldBounds;
                }
                else
                {
                    bounds.Encapsulate(worldBounds);
                }
            }

            return bounds;
        }

        public static Vector3 GetCombinedParentPositions(SplineObject so)
        {
            Vector3 value = Vector3.zero;

            for (int i = 0; i < 25; i++)
            {
                if (so == null)
                    break;

                value += so.localSplinePosition;
                so = so.SoParent;
            }

            return value;
        }

        public static Vector3 GetCombinedParentScales(SplineObject so)
        {
            Vector3 value = Vector3.one;

            for (int i = 0; i < 25; i++)
            {
                if (so == null)
                    break;

                value = Vector3.Scale(value, so.transform.localScale);
                so = so.SoParent;
            }

            return value;
        }

        public static Quaternion GetCombinedParentRotations(SplineObject so)
        {
            Quaternion value = Quaternion.identity;

            for (int i = 0; i < 25; i++)
            {
                if (so == null)
                    break;

                value = so.localSplineRotation * value;
                so = so.SoParent;
            }

            return value;
        }

        public static float4x4 GetCombinedParentMatrixs(SplineObject so, bool forceSplineSpace = false)
        {
            float4x4 matrix = float4x4.identity;

            for (int i = 0; i < 25; i++)
            {
                if (so == null)
                    break;

                if (forceSplineSpace || so.Type == SplineObjectType.DEFORMATION)
                    matrix = math.mul(float4x4.TRS(so.localSplinePosition, so.localSplineRotation, so.transform.localScale), matrix);
                else
                    matrix = math.mul(float4x4.TRS(so.transform.localPosition, so.transform.localRotation, so.transform.localScale), matrix);

                so = so.SoParent;
            }

            return matrix;
        }

        public static int GetCombinedParentHashCodes(SplineObject so)
        {
            int haschCode = 0;

            for (int i = 0; i < 25; i++)
            {
                so = so.SoParent;

                if (so == null)
                    break;

                if (so.Type != SplineObjectType.DEFORMATION)
                    break;

                haschCode += so.GetHashCode();
            }

            return haschCode;
        }

        public static SplineObject GetRootSplineObject(Transform transform)
        {
            SplineObject so = null;

            for (int i = 0; i < 25; i++)
            {
                if (transform == null)
                    break;

                SplineObject so2 = transform.GetComponent<SplineObject>();

                if (so2 != null)
                    so = so2;

                transform = transform.parent;
            }

            return so;
        }

        public static SplineObject GetRootSplineObject(SplineObject so)
        {
            if (so == null)
                return null;

            for (int i = 0; i < 25; i++)
            {
                if (so.SoParent == null)
                    break;

                so = so.SoParent;
            }

            return so;
        }
    }
}