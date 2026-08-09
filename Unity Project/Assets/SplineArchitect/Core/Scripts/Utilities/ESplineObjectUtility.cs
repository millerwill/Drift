// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SplineObjectUtility.cs
//
// Author: Mikael Danielsson
// Date Created: 11-09-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

#if UNITY_EDITOR
using UnityEngine;

namespace SplineArchitect.Utility
{
    internal class ESplineObjectUtility
    {
        internal static bool TryAttacheOnTransformEditor(Spline spline, Transform transform, bool skipTransforming, bool skipUndo = false)
        {
            //Ignore Splines
            if (transform.GetComponent<Spline>() != null)
                return false;

            SplineObject soParent = null;
            if (transform.parent != null) soParent = transform.parent.GetComponent<SplineObject>();

            SplineObject newSo = transform.GetComponent<SplineObject>();

            //If part of hierarchy this data needs to be the same for all spline objects in the hierarchy.
            if (newSo != null && soParent != null)
            {
                UnityEditor.Undo.RecordObject(newSo, "Attached SplineObject");
                newSo.AlignToEnd = soParent.AlignToEnd;
                newSo.componentMode = soParent.componentMode;
            }
            //Create so
            else if (newSo == null)
            {
                if (skipUndo)
                {
                    newSo = transform.gameObject.AddComponent<SplineObject>();
                }
                else
                {
                    newSo = UnityEditor.Undo.AddComponent<SplineObject>(transform.gameObject);
                    UnityEditor.Undo.RecordObject(newSo, "Attached SplineObject");
                }

                if (skipTransforming)
                {
                    newSo.localSplinePosition = newSo.transform.localPosition;
                    newSo.localSplineRotation = newSo.transform.localRotation;
                }
                else
                {
                    if (transform.gameObject.GetComponentCount() > 2 || transform.childCount > 0)
                    {
                        newSo.splinePosition = spline.WorldPositionToSplinePosition(newSo.transform.position, 12);
                        newSo.splineRotation = spline.WorldRotationToSplineRotation(newSo.transform.rotation, newSo.splinePosition.z / spline.Length);
                        newSo.transform.localPosition = newSo.localSplinePosition;
                        newSo.transform.localRotation = newSo.localSplineRotation;
                    }
                    else
                    {
                        newSo.splinePosition = Vector3.zero;
                        newSo.splineRotation = Quaternion.identity;
                    }
                }

                newSo.Monitor.UpdatePosRotSplineSpace();

                if (newSo.Type == SplineObjectType.DEFORMATION)
                    spline.MarkEditorCacheDirty();

                return true;
            }

            return false;
        }

        internal static Mesh GetOriginMeshFromMeshNameId(Mesh mesh)
        {
            Mesh originMesh = null;

            if (mesh == null)
                return null;

            string[] data = mesh.name.Split('*');
            if (data.Length == 5)
            {
#if UNITY_6000_4_OR_NEWER
                if (ulong.TryParse(data[1], out ulong id))
                {
                    Object obj = UnityEditor.EditorUtility.EntityIdToObject(EntityId.FromULong(id));
                    if (obj is Mesh) originMesh = obj as Mesh;
                }
#elif UNITY_6000_3_OR_NEWER
                if (int.TryParse(data[1], out int id))
                {
                    Object obj = UnityEditor.EditorUtility.EntityIdToObject(id);
                    if (obj is Mesh) originMesh = obj as Mesh;
                }
#else
                if (int.TryParse(data[1], out int id))
                {
                    Object obj = UnityEditor.EditorUtility.InstanceIDToObject(id);
                    if (obj is Mesh) originMesh = obj as Mesh;
                }
#endif

            }

            return originMesh;
        }
    }
}
#endif