// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: Spline_Editor.cs
//
// Author: Mikael Danielsson
// Date Created: 25-03-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;

using UnityEngine;

using SplineArchitect.Utility;

using Vector3 = UnityEngine.Vector3;

namespace SplineArchitect
{
    public partial class Spline : MonoBehaviour
    {
#if UNITY_EDITOR

        // =====================
        // Editor-only (ignored at runtime)
        // =====================

        // General, stored
        [HideInInspector, SerializeField] internal string editorId;

        // Selection data, runtime
        [NonSerialized] internal int indicatorSegment;
        [NonSerialized] internal Vector3 indicatorPosition;
        [NonSerialized] internal float indicatorTime;
        [NonSerialized] internal Vector3 indicatorDirection;
        [NonSerialized] internal float indicatorDistanceToSpline;

        // General, runtime
        [NonSerialized] public bool editorDisableOnChildrenChanged;
        [NonSerialized] internal bool editorInitialized;
        [NonSerialized] private bool editorInitializedLinks;
        [NonSerialized] internal ScriptableObjects.SplineUndoState undoState;
        internal int vertices { get; private set; }
        internal int deformations { get; private set; }
        internal int deformationsInBuild { get; private set; }
        internal int followers { get; private set; }
        internal int followersInBuild { get; private set; }
        internal float deformationsMemoryUsage { get; private set; }

        private void Awake()
        {
            editorId = UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(transform).targetObjectId.ToString();
        }

        private void OnTransformChildrenChanged()
        {
            if (editorDisableOnChildrenChanged)
                return;

            Monitor.EditorChildCountChange(out int dif, true);

            if (dif > 0)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    Attach(child, false);
                }
            }

            bool Attach(Transform transform, bool skipTransforming)
            {
                bool attached = ESplineObjectUtility.TryAttacheOnTransformEditor(this, transform, skipTransforming);

                SplineObject so = transform.GetComponent<SplineObject>();

                if (so == null || so.Type != SplineObjectType.DEFORMATION)
                    return attached;

                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);

                    if (child == transform)
                        continue;

                    bool attachedChild = Attach(child, true);

                    if (!Application.isPlaying && attached != attachedChild)
                        Debug.LogWarning("[Spline Architect] All GameObjects parented to a spline or deformation must either have a SplineObject component, or none at all. " +
                                         "Failing to do so may result in incorrect positions but will otherwise work fine.");
                }

                return attached;
            }
        }

        internal void MarkEditorCacheDirty()
        {
            undoState.editorCacheVersion++;
        }

        internal bool IsEditorCacheDirty()
        {
            return undoState.editorCacheVersion != undoState.oldEditorCacheVersion;
        }

        private void CopySettingsFromClosestSegment(Segment segment, int index)
        {
            if (segments.Count > 1)
            {
                Segment closest = null;
                if (index == 0)
                {
                    closest = segments[index + 1];
                }
                else if (index == segments.Count - 1)
                {
                    closest = segments[index - 1];
                }
                else
                {
                    float d1 = Vector3.Distance(segments[index].GetPosition(ControlHandle.ANCHOR), segments[index - 1].GetPosition(ControlHandle.ANCHOR));
                    float d2 = Vector3.Distance(segments[index].GetPosition(ControlHandle.ANCHOR), segments[index + 1].GetPosition(ControlHandle.ANCHOR));

                    if (d1 > d2)
                        closest = segments[index + 1];
                    else
                        closest = segments[index - 1];
                }

                if (closest == null)
                {
                    Debug.LogWarning("[Spline Architect] Could not find closest segment!");
                    return;
                }

                segment.SetInterpolationType(closest.GetInterpolationType());
                segment.IsStatic = closest.IsStatic;
            }
        }

        private void FixUnityPrefabBoundsCase()
        {
            // Bounds are calculated using the wrong transform.position during OnEnable
            // when opening a prefab. They are calculated as if the spline's transform.localPosition were
            // the world position, completely ignoring any parent transforms.
            //
            // This issue seems to be fixed in later Unity versions.
            if (EHandlePrefab.IsPrefabStageActive())
            {
                foreach (Spline spline in HandleRegistry.GetSplinesUnsafe())
                {
                    if (spline != null) spline.CalculateControlpointBounds();
                }
            }
        }

        private bool ValidForPlaymode(bool validOnSelected = false)
        {
            if (EHandleEvents.waitForEditor)
                return false;

            if (!Application.isPlaying)
                return false;

            if (!validOnSelected && (EHandleEvents.selectedSpline == this || EHandleEvents.isSplineConnectorSelected))
                return false;

            if (EHandleEvents.dragActive)
                return false;

            if (segments.Count < 2)
                return false;

            return true;
        }

        internal Vector3 GetPositionFastWorld(float time)
        {
            if (PositionMap.Length <= 0)
                return GetPosition(time, Space.World);

            float indexValue = time / GetSplineResolution();

            int lmIndex = (int)Mathf.Floor(indexValue);
            int lmIndex2 = lmIndex + 1;
            float mod = indexValue;
            if (indexValue > 1) mod = indexValue % lmIndex;

            if (lmIndex > PositionMap.Length - 1)
                lmIndex = PositionMap.Length - 1;

            if (lmIndex < 0)
                lmIndex = 0;

            if (lmIndex2 > PositionMap.Length - 1)
                lmIndex2 = PositionMap.Length - 1;

            if (lmIndex2 < 0)
                lmIndex2 = 0;

            return PositionMap[lmIndex] + ((PositionMap[lmIndex2] - PositionMap[lmIndex]) * mod);
        }

        internal void UpdateInfo()
        {
            vertices = 0;
            deformations = 0;
            deformationsInBuild = 0;
            followers = 0;
            followersInBuild = 0;

            for (int i = allSplineObjects.Count - 1; i >= 0; i--)
            {
                SplineObject so = allSplineObjects[i];

                if (so == null)
                    continue;

                so.UpdateInfo();

                if (so.Type == SplineObjectType.NONE)
                    continue;

                GameObject root = EHandlePrefab.GetOutermostPrefabRoot(so.gameObject);
                bool partOfPrefab = EHandlePrefab.IsPartOfAnyPrefab(so.gameObject);
                bool componentIsInBuild = ((!partOfPrefab || root == so.gameObject) && so.componentMode != ComponentMode.REMOVE_FROM_BUILD) ||
                                          (partOfPrefab && root != so.gameObject && (root.hideFlags & HideFlags.DontSaveInBuild) == 0);

                //Get total SplineObjects
                if (so.Type == SplineObjectType.FOLLOWER)
                {
                    followers++;

                    if (componentIsInBuild)
                        followersInBuild++;
                }
                else
                {
                    deformations += so.deformations;
                    vertices += so.deformedVertecies;

                    if (componentIsInBuild)
                        deformationsInBuild += so.deformations;
                }
            }

            //Calculate total deformed mesh memory
            float size = 0;
            foreach (SplineObject so in allSplineObjects)
            {
                if (so.Type != SplineObjectType.DEFORMATION)
                    continue;

                if (so.meshMode == MeshMode.DO_NOTHING)
                    continue;

                for (int i = 0; i < so.MeshContainerCount; i++)
                {
                    MeshContainer mc = so.GetMeshContainerAtIndex(i);
                    Component meshContainerComponent = mc.GetMeshComponent();

                    if (meshContainerComponent == null || meshContainerComponent.transform == null)
                        continue;

                    //Skip mc:s that uses the same mesh as so.meshContainers[0].
                    if (mc != so.GetMeshContainerAtIndex(0) && mc.GetInstanceMesh() == so.GetMeshContainerAtIndex(0).GetInstanceMesh())
                        continue;

                    size += mc.GetDataUsage();
                }
            }
            deformationsMemoryUsage = Mathf.Round(size);
        }

        internal bool IsHiddenInSceneView()
        {
            return UnityEditor.SceneVisibilityManager.instance.IsHidden(gameObject);
        }

        internal bool IsPickingDisabled()
        {
            return UnityEditor.SceneVisibilityManager.instance.IsPickingDisabled(gameObject);
        }
#endif
    }
}
