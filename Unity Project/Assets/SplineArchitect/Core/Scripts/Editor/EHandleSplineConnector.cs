// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleSplineConnector.cs
//
// Author: Mikael Danielsson
// Date Created: 23-05-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace SplineArchitect
{
    public static class EHandleSplineConnector
    {
        private static List<SplineConnector> markedForDeletion = new List<SplineConnector>();
        private static List<Spline> splinesContainer = new List<Spline>();

        internal static void UpdateGlobal()
        {
            markedForDeletion.Clear();

            foreach (SplineConnector sc in HandleRegistry.GetSplineConnectorsUnsafe())
            {
                if (sc == null)
                {
                    markedForDeletion.Add(sc);
                    continue;
                }

                Transform selected = Selection.activeTransform;
                bool selectedIsParent = false;

                for (int i2 = 0; i2 < 25; i2++)
                {
                    if (selected == null)
                        break;

                    if (selected == sc.transform.parent)
                    {
                        selectedIsParent = true;
                        break;
                    }

                    selected = selected.parent;
                }

                splinesContainer.Clear();
                EHandleSelection.GetAllSelectedSplinesNonAlloc(splinesContainer);
                bool selectedIsLinkedSpline = false;

                foreach(Spline spline in splinesContainer)
                {
                    foreach (Segment s in spline.segments)
                    {
                        if (s.splineConnector != null && s.splineConnector == sc)
                        {
                            selectedIsLinkedSpline = true;
                            break;
                        }
                    }
                }

                if (!selectedIsLinkedSpline && !selectedIsParent && sc != EHandleSelection.selectedSplineConnector && !EHandleSelection.selectedSplineConnectors.Contains(sc))
                    continue;

                bool scPosChange = sc.Monitor.PosChange();
                bool scRotChange = sc.Monitor.RotChange();
                bool segmentOffsetChange = sc.Monitor.SegmentOffsetChange();

                for (int i = sc.ConnectionCount - 1; i >= 0; i--)
                {
                    Segment s = sc.GetConnectionAtIndex(i);

                    if (s == null)
                        continue;

                    if (s.localSpace == null)
                        continue;

                    if (s.linkTarget != LinkTarget.SPLINE_CONNECTOR)
                    {
                        s.SplineConnector.RemoveConnection(s);
                        continue;
                    }

                    bool splineTransformMoved = s.splineParent.Monitor.EditorTransformChange();
                    if (splineTransformMoved) s.splineParent.MarkEditorCacheDirty();

                    if (scPosChange || scRotChange || segmentOffsetChange || splineTransformMoved)
                    {
                        sc.AlignSegment(s);
                        EHandleEvents.InvokeAfterSplineConnectorAlignSegment(sc, s);

                        //Add to selection if moving parent or grandparent, and so on... of spline connector.
                        //Maybe not the best way to do it but works and has good performence compeared to other solutions I had.
                        if (!EHandleSelection.selectedSplineConnectors.Contains(sc))
                            EHandleSelection.selectedSplineConnectors.Add(sc);
                    }
                }
            }

            for(int i = 0; i < markedForDeletion.Count; i++)
            {
                HandleRegistry.RemoveSplineConnector(markedForDeletion[i]);
            }
        }

        public static SplineConnector CreatedForContext(GameObject go)
        {
            go.name = $"SplineConnector ({HandleRegistry.GetSplineConnectorsUnsafe().Count + 1})";
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                SceneManager.MoveGameObjectToScene(go, prefabStage.scene);
                EHandleUndo.RegisterCreatedObject(go, "Created SplineConnector");
                EHandleUndo.SetTransformParent(go.transform, prefabStage.prefabContentsRoot.transform, "Created SplineConnector");
            }
            else
            {
                EHandleUndo.RegisterCreatedObject(go, "Created SplineConnector");
            }

            return EHandleUndo.AddComponent<SplineConnector>(go);
        }
    }
}
