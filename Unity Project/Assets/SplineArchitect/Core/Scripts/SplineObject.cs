// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SplineObject.cs
//
// Author: Mikael Danielsson
// Date Created: 28-03-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;
using Unity.Mathematics;

using SplineArchitect.Monitor;
using SplineArchitect.Utility;

namespace SplineArchitect
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("SplineArchitect/SplineObject")]
    public partial class SplineObject : MonoBehaviour
    {
        public const int dataUsage = 32 + 
                                     1 + 12 + 16 + 12 + 1 + 1 + 1 + 8 + 8 + 40 + 40 + 1 + 4 + 1 + MonitorSplineObject.dataUsage + 1 + 1;

        // Static data
        public static SplineObjectType defaultType = SplineObjectType.DEFORMATION;
        private static List<Vector3> normalsContainer = new List<Vector3>();
        private static List<Vector4> tangentsContainer = new List<Vector4>();
        private static List<int> trianglesContainer = new List<int>();
        private static List<Renderer> rendererContainer = new List<Renderer>();

        // Public stored data
        [HideInInspector] public Vector3 localSplinePosition;
        [HideInInspector] public Quaternion localSplineRotation = Quaternion.identity;

        // Private stored data
        [SerializeField, HideInInspector] internal ComponentMode componentMode = ComponentMode.REMOVE_FROM_BUILD;
        [SerializeField, HideInInspector] internal MeshMode meshMode = MeshMode.SAVE_IN_BUILD;
        [SerializeField, HideInInspector] internal SnapSettings snapSettings;
        [SerializeField, HideInInspector] private float lockPosition;
        [SerializeField, HideInInspector] private RotationMode rotationMode = RotationMode.FOLLOW_SPLINE;
        [SerializeField, HideInInspector] private SplineObjectType type = SplineObjectType.NOT_SET;
        [SerializeField, HideInInspector] private NormalType normalType;
        [SerializeField, HideInInspector] private bool mirrorDeformation;
        [SerializeField, HideInInspector] private bool alignToEnd;
        [SerializeField, HideInInspector] private bool skipTangents;
        [SerializeField, HideInInspector] private List<MeshContainer> meshContainers;
        [SerializeField, HideInInspector] private Spline splineParent;
        [SerializeField, HideInInspector] private SplineObject soParent;
        [SerializeField, HideInInspector] private bool projectToSurface;
        [SerializeField, HideInInspector] private LayerMask projectToSurfaceMask = -1;

        // Private runtime data
        [NonSerialized] internal int spreadFrame = -1;
        [NonSerialized] internal ulong assignedWorkerId;
        [NonSerialized] private MonitorSplineObject monitor;
        [NonSerialized] private int version;
        [NonSerialized] private int oldVersion;
        [NonSerialized] private bool meshRendered = true;
        [NonSerialized] private bool canInternallyChangeRenderState = true;

        // Properties
        internal MonitorSplineObject Monitor => monitor;
        public int MeshContainerCount => meshContainers != null ? meshContainers.Count : 0;
        public Vector3 splinePosition
        {
            get
            {
                if (soParent == null) return localSplinePosition;
                return math.transform(SplineObjectUtility.GetCombinedParentMatrixs(soParent), localSplinePosition);
            }
            set
            {
                localSplinePosition = math.transform(math.inverse(SplineObjectUtility.GetCombinedParentMatrixs(soParent)), value);
            }
        }
        public Quaternion splineRotation
        {
            get
            {
                if (soParent == null) return localSplineRotation;
                return SplineObjectUtility.GetCombinedParentRotations(soParent) * localSplineRotation;
            }
            set
            {
                localSplineRotation = Quaternion.Inverse(SplineObjectUtility.GetCombinedParentRotations(soParent)) * value;
            }
        }
        public Spline SplineParent => splineParent;
        public SplineObject SoParent => soParent;
        public bool AlignToEnd
        {
            get => alignToEnd;
            set
            {
                if (value == alignToEnd)
                    return;

                alignToEnd = value;
                MarkVersionDirty();
            }
        }
        public bool SkipTangents
        {
            get
            {
                if (meshContainers != null && meshContainers.Count > 0 && meshContainers[0].MeshComponentIsMeshFilter())
                    return skipTangents;

                return true;
            }
            set
            {
                if (value == skipTangents)
                    return;

                skipTangents = value;
                MarkVersionDirty();
            }
        }
        public bool ProjectToSurface
        {
            get => projectToSurface;
            set
            {
                if (value == projectToSurface)
                    return;

                projectToSurface = value;
                MarkVersionDirty();
            }
        }
        public bool MirrorDeformation
        {
            get => mirrorDeformation;
            set
            {
                if (mirrorDeformation == value)
                    return;

                mirrorDeformation = value;
                MarkVersionDirty();
            }
        }
        public NormalType NormalType
        {
            get => normalType;
            set
            {
                if (normalType == value)
                    return;

                normalType = value;
                MarkVersionDirty();
            }
        }
        public SplineObjectType Type
        {
            get => type;
            set
            {
                if (type == value)
                    return;

                if (value == SplineObjectType.NOT_SET)
                    throw new ArgumentException("SplineObjectType cannot be NOT_SET.", nameof(value));

                if(value == SplineObjectType.DEFORMATION)
                    rotationMode = RotationMode.FOLLOW_SPLINE;

                type = value;
                MarkVersionDirty();
            }
        }
        public SnapSettings SnapSettings
        {
            get => snapSettings;
            set
            {
                if (GeneralUtility.IsEqual(snapSettings.startSnapDistance, value.startSnapDistance) &&
                    GeneralUtility.IsEqual(snapSettings.startSnapOffset, value.startSnapOffset) &&
                    GeneralUtility.IsEqual(snapSettings.endSnapDistance, value.endSnapDistance) &&
                    GeneralUtility.IsEqual(snapSettings.endSnapOffset, value.endSnapOffset) &&
                    GeneralUtility.IsEqual(snapSettings.snapTargetPoint, value.snapTargetPoint) &&
                    snapSettings.snapMode == value.snapMode)
                    return;

                snapSettings = value;
                MarkVersionDirty();
            }
        }
        public float LockPosition
        {
            get => lockPosition;
            set
            {
                if (GeneralUtility.IsEqual(lockPosition, value))
                    return;

                lockPosition = Mathf.Clamp01(value);
                MarkVersionDirty();
            }
        }
        public RotationMode RotationMode
        {
            get => rotationMode;
            set
            {
                if (rotationMode == value)
                    return;

                if (type != SplineObjectType.FOLLOWER)
                {
                    Debug.LogError("[Spline Architect] RotationMode can only be set on SplineObjects of type FOLLOWER.");
                    return;
                }

                rotationMode = value;
                MarkVersionDirty();
            }
        }
        public LayerMask ProjectToSurfaceMask
        { 
            get => projectToSurfaceMask;
            set
            {
                if (projectToSurfaceMask == value)
                    return;

                projectToSurfaceMask = value;
                MarkVersionDirty();
            }
        }

        /// <summary>
        /// Enables or disables rendering for the mesh associated with this SplineObject.
        /// This affects the MeshRenderer on the same GameObject as well as all Renderer
        /// components found on its children.
        /// </summary>
        public void RenderMesh(bool value)
        {
            SetRenderMesh(value);
            canInternallyChangeRenderState = value;
        }

        /// <summary>
        /// Returns whether rendering is enabled for this SplineObject, set by RenderMesh().
        /// </summary>
        public bool IsMeshRendered()
        {
            return meshRendered;
        }

        /// <summary>
        /// Returns true if this spline object is a parent, grandparent, 
        /// or any higher-level ancestor of the given spline object.
        /// </summary>
        public bool IsAncestorOf(SplineObject so)
        {
            SplineObject parent = so.soParent;

            for (int i = 0; i < 25; i++)
            {
                if (parent == null)
                    return false;

                if (parent == this)
                    return true;

                parent = parent.soParent;
            }

            return false;
        }

        /// <summary>
        /// Gets the MeshContainer at a specific index.
        /// A MeshContainer that handles a MeshFilter will always have index 0.
        /// </summary>
        public MeshContainer GetMeshContainerAtIndex(int index)
        {
            return meshContainers[index];
        }

        /// <summary>
        /// Scans the GameObject for MeshFilter and MeshCollider components 
        /// and creates MeshContainer entries for any that are not yet registered.
        /// Call SyncInstanceMeshesFromCache() after this to create 
        /// instance meshes for each MeshContainer created by this function.
        /// </summary>
        public void SyncMeshContainers()
        {
            for (int i = 0; i < gameObject.GetComponentCount(); i++)
            {
                Component component = gameObject.GetComponentAtIndex(i);
                MeshFilter meshFilter = component as MeshFilter;
                MeshCollider meshCollider = component as MeshCollider;

                if (meshFilter == null && meshCollider == null)
                    continue;

                Mesh sharedMesh = null;
                if (meshFilter != null) sharedMesh = meshFilter.sharedMesh;
                else if (meshCollider != null) sharedMesh = meshCollider.sharedMesh;

                if (sharedMesh == null)
                    continue;

                bool allreadyExists = false;
                foreach (MeshContainer mc2 in meshContainers)
                {
                    if (mc2 != null && mc2.ContainsMeshComponent(component))
                    {
                        allreadyExists = true;
                        break;
                    }
                }

                if (allreadyExists)
                    continue;

                MeshContainer mc = new MeshContainer(component);
                mc.RenderMesh(meshRendered);
                AddMeshContainer(mc);
            }
        }

        /// <summary>
        /// Updates the instance meshes for all MeshContainers.
        /// Call this after changing the mesh for a MeshContainer
        /// using meshContainer.SetOriginMesh(), or after running
        /// SyncMeshContainers() on this SplineObject.
        /// This will fetch or create a new cached instance mesh
        /// and ensure that it is properly deformed by the spline.
        /// </summary>
        public void SyncInstanceMeshesFromCache()
        {
            foreach (MeshContainer mc in meshContainers)
            {
                if (mc == null)
                    continue;

                if (mc.GetMeshComponent() == null)
                    continue;

                Mesh oldInstanceMesh = mc.GetInstanceMesh();
                Mesh instanceMesh = HandleCachedResources.FetchInstanceMesh(mc);

                mc.SetInstanceMesh(instanceMesh);

                if (oldInstanceMesh == instanceMesh)
                    continue;

                MarkVersionDirty();
                RenderMeshInternal(false);
            }
        }

        internal void RenderMeshInternal(bool value)
        {
            if (!canInternallyChangeRenderState)
                return;

            SetRenderMesh(value);
        }

        internal void AdjustAutoResolution()
        {
            for(int i = 0; i < MeshContainerCount; i++)
            {
                MeshContainer mc = GetMeshContainerAtIndex(i);
                if(mc.AutoResolution)
                {
                    float greatestAngle = Mathf.Abs(GetGreatestAngleDif(mc));
                    greatestAngle = Mathf.Clamp(greatestAngle, 0, 90);
                    float bias = 2 - mc.AutoBias;
                    bias = Mathf.Max(bias, 0.01f);

                    if(greatestAngle < 3 * bias)
                    {
                        mc.Resolution = 0;
                        continue;
                    }

                    float t = greatestAngle / 120;
                    t = EasingUtility.EvaluateEasing(t, Easing.EASE_OUT_CIRC);
                    float minEdgeLength = Mathf.Lerp(70f, 0.1f, t) * bias;
                    int oldResolution = mc.Resolution;

                    for(int i2 = 1; i2 < 15; i2++)
                    {
                        mc.Resolution = i2;
                        mc.UpdateKeys();
                        float longestEdge = HandleCachedResources.GetLongestEdgeFromModifiedOriginalMesh(mc) * 3.33f;
                        if(longestEdge < minEdgeLength)
                            break;
                    }

                    if(oldResolution != mc.Resolution)
                    {
#if UNITY_EDITOR
                        EHandleEvents.MarkForInfoUpdate(SplineParent);
#endif
                    }
                }
            }

            float GetGreatestAngleDif(MeshContainer mc)
            {
                Mesh originalMesh = mc.GetOriginMesh();
                Bounds bounds = GeneralUtility.TransformBounds(originalMesh.bounds, SplineObjectUtility.GetCombinedParentMatrixs(this));
                float oneMeterTime = bounds.size.z / 3 / splineParent.Length;
                float startTime = (bounds.center.z - bounds.extents.z) / splineParent.Length;
                float endTime = (bounds.center.z + bounds.extents.z) / splineParent.Length;
                float ft1 = splineParent.TimeToFixedTime(endTime - oneMeterTime);
                float ft2 = splineParent.TimeToFixedTime(endTime);

                float greatestAngle = Vector3.Angle(splineParent.GetDirection(ft1), 
                                            splineParent.GetDirection(ft2));

                for(float time = startTime; time < endTime; time += oneMeterTime / 3)
                {
                    ft1 = splineParent.TimeToFixedTime(time);
                    float t1 = time + oneMeterTime;
                    if(t1 > endTime) t1 = endTime;
                    ft2 = splineParent.TimeToFixedTime(t1);
                    float angle = Vector3.Angle(splineParent.GetDirection(ft1), 
                                          splineParent.GetDirection(ft2));

                    if(angle > greatestAngle)
                        greatestAngle = angle;
                }

                return greatestAngle;
            }
        }

        internal void MarkVersionDirty()
        {
#if UNITY_EDITOR
            // Is here becouse when you Change SplineObjectType and uses undo, the type will not
            // change back to its correct state becouse the version is makred dirty
            if (EHandleEvents.undoActive)
                return;

            undoState.version++;
#endif
            version++;
        }

        internal bool IsVersionDirty()
        {
            bool value = oldVersion != version;
            oldVersion = version;

#if UNITY_EDITOR
            if (!value) value = undoState.oldVersion != undoState.version;
            undoState.oldVersion = undoState.version;
#endif
            return value;
        }

        internal void AddMeshContainer(MeshContainer mc)
        {
            if (mc.MeshComponentIsMeshFilter()) meshContainers.Insert(0, mc);
            else meshContainers.Add(mc);
        }

        internal void RemoveMeshContainer(MeshContainer mc)
        {
            meshContainers.Remove(mc);
        }

        internal void RemoveMeshContainerAt(int index)
        {
            meshContainers.RemoveAt(index);
        }

        internal void UpdateExternalComponents(bool useOriginMesh = false)
        {
            for (int i = 0; i < gameObject.GetComponentCount(); i++)
            {
                Component component = gameObject.GetComponentAtIndex(i);

                if (component == null)
                    continue;

                //Update primitive colliders
                if (component is Collider)
                {
                    Mesh mesh = null;

                    if (meshContainers != null && meshContainers.Count > 0)
                    {
                        mesh = meshContainers[0].GetInstanceMesh();

                        if (useOriginMesh)
                            mesh = meshContainers[0].GetOriginMesh();

                        if (mesh == null)
                        {
                            for (int i2 = 0; i2 < gameObject.GetComponentCount(); i2++)
                            {
                                MeshFilter meshFilter = gameObject.GetComponentAtIndex(i) as MeshFilter;
                                MeshCollider meshCollider = gameObject.GetComponentAtIndex(i) as MeshCollider;

                                if (meshFilter != null)
                                {
                                    mesh = meshFilter.sharedMesh;
                                    break;
                                }

                                if (meshCollider != null)
                                {
                                    mesh = meshCollider.sharedMesh;
                                    break;
                                }
                            }
                        }
                    }

                    Collider collider = component as Collider;

                    if (collider is BoxCollider)
                    {
                        BoxCollider boxCollider = collider as BoxCollider;
                        if (mesh != null)
                        {
                            boxCollider.center = mesh.bounds.center;
                            boxCollider.size = mesh.bounds.size;
                        }
                        else
                        {
                            Vector3 center = transform.position;
                            if (splineParent != null) center = transform.InverseTransformPoint(splineParent.SplinePositionToWorldPosition(splinePosition));
                            boxCollider.center = center;
                        }
                    }
                    else if (collider is SphereCollider)
                    {
                        SphereCollider sphereCollider = collider as SphereCollider;
                        if (mesh != null)
                        {
                            sphereCollider.center = mesh.bounds.center;
                            float r = Mathf.Max(mesh.bounds.extents.x, mesh.bounds.extents.y);
                            sphereCollider.radius = Mathf.Max(r, mesh.bounds.extents.z);

                        }
                        else
                        {
                            Vector3 center = transform.position;
                            if (splineParent != null) center = transform.InverseTransformPoint(splineParent.SplinePositionToWorldPosition(splinePosition));
                            sphereCollider.center = center;
                        }
                    }
                    else if (collider is CapsuleCollider)
                    {
                        CapsuleCollider capsuleCollider = collider as CapsuleCollider;
                        if (mesh != null)
                        {
                            capsuleCollider.center = mesh.bounds.center;
                            capsuleCollider.radius = Mathf.Max(mesh.bounds.extents.x, mesh.bounds.extents.z);
                            capsuleCollider.height = Mathf.Max(mesh.bounds.size.y, capsuleCollider.radius);
                        }
                        else
                        {
                            Vector3 center = transform.position;
                            if (splineParent != null) center = transform.InverseTransformPoint(splineParent.SplinePositionToWorldPosition(splinePosition));
                            capsuleCollider.center = center;
                        }
                    }
                }
                //Update LODGroup
                else if (component is LODGroup)
                {
                    LODGroup lodGroup = component as LODGroup;
                    lodGroup.RecalculateBounds();

#if UNITY_EDITOR
                    if (lodGroup.animateCrossFading && lodGroup.fadeMode != LODFadeMode.None && type == SplineObjectType.DEFORMATION)
                        Debug.LogWarning("[Spline Architect] Using Animate Cross-fading on a Deformation with LOD Group may have undesired consequences.");
#endif
                }
            }
        }

        internal void ClearMeshContainers()
        {
            meshContainers.Clear();
        }

        internal void CacheUntrackedInstanceMeshes()
        {
            foreach (MeshContainer mc in meshContainers)
            {
                if (mc == null)
                    continue;

                Mesh instanceMesh = mc.GetInstanceMesh();
                Mesh originMesh = mc.GetOriginMesh();

                if (instanceMesh == null || originMesh == null)
                    continue;

                if (instanceMesh == originMesh)
                    continue;

                if (HandleCachedResources.IsInstanceMeshCached(instanceMesh))
                    continue;

                HandleCachedResources.AddOrUpdateInstanceMesh(mc);
                mc.SetInstanceMesh(instanceMesh);

                if(canInternallyChangeRenderState)
                    mc.RenderMesh(true);
            }
        }

        internal void SetInstanceMeshesToOriginMesh()
        {
            foreach (MeshContainer mc in meshContainers)
            {
                if (mc == null)
                    continue;

                Mesh originMesh = mc.GetOriginMesh();
                if (originMesh == null) continue;

                mc.SetInstanceMeshToOriginMesh();
            }
        }

        internal void DestroyAllInstanceMeshes()
        {
            foreach (MeshContainer mc in meshContainers)
            {
                Mesh instanceMesh = mc.GetInstanceMesh();

                if (instanceMesh == null)
                    continue;

                Component component = mc.GetMeshComponent();

                if (component is MeshCollider)
                {
                    MeshCollider meshCollider = component as MeshCollider;
                    meshCollider.sharedMesh = null;
                }
                else if (component is MeshFilter)
                {
                    MeshFilter meshFilter = component as MeshFilter;
                    meshFilter.sharedMesh = null;
                }
            }
        }

        internal void SetOriginNormalsOnAll()
        {
            foreach (MeshContainer mc in meshContainers)
            {
                Mesh sharedMesh = mc.GetInstanceMesh();
                Mesh originMesh = mc.GetOriginMesh();

                if (sharedMesh == null)
                    continue;

                if (sharedMesh == originMesh)
                    continue;

                if(mc.Resolution > 0)
                    originMesh = HandleCachedResources.FetchModifiedOriginalMesh(mc);

                normalsContainer.Clear();
                originMesh.GetNormals(normalsContainer);
                sharedMesh.SetNormals(normalsContainer);
            }
        }

        internal void SetOriginTangentsOnAll()
        {
            foreach (MeshContainer mc in meshContainers)
            {
                Mesh sharedMesh = mc.GetInstanceMesh();
                Mesh originMesh = mc.GetOriginMesh();

                if (sharedMesh == null)
                    continue;

                if (sharedMesh == originMesh)
                    continue;

                if(mc.Resolution > 0)
                    originMesh = HandleCachedResources.FetchModifiedOriginalMesh(mc);

                tangentsContainer.Clear();
                originMesh.GetTangents(tangentsContainer);
                sharedMesh.SetTangents(tangentsContainer);
            }
        }

        internal void SetOriginTrianglesOnAll()
        {
            foreach (MeshContainer mc in meshContainers)
            {
                Mesh sharedMesh = mc.GetInstanceMesh();
                Mesh originMesh = mc.GetOriginMesh();

                if (sharedMesh == null)
                    continue;

                if (sharedMesh == originMesh)
                    continue;

                if(mc.Resolution > 0)
                    originMesh = HandleCachedResources.FetchModifiedOriginalMesh(mc);

                for (int i = 0; i < originMesh.subMeshCount; i++)
                {
                    trianglesContainer.Clear();
                    originMesh.GetTriangles(trianglesContainer, i);
                    sharedMesh.SetTriangles(trianglesContainer, i);
                }
            }
        }

        internal void SetSeamlessTrianglesOnAll()
        {
            foreach (MeshContainer mc in meshContainers)
            {
                MeshUtility.SetSeamlessTriangles(mc);
            }
        }

        internal void ReverseTrianglesOnAll()
        {
            foreach (MeshContainer mc in meshContainers)
            {
                Mesh sharedMesh = mc.GetInstanceMesh();
                Mesh originMesh = mc.GetOriginMesh();

                if (sharedMesh == null)
                    continue;

                if (sharedMesh == originMesh)
                    continue;

                MeshUtility.ReverseTriangles(sharedMesh);
            }
        }

        internal SnapData CalculateSnapData()
        {
            SnapData snapData = new SnapData();

            if (meshContainers == null || meshContainers.Count == 0)
            {
                return snapData;
            }

            Bounds localBounds = meshContainers[0].GetOriginMesh().bounds;
            Bounds localParentBounds = GeneralUtility.TransformBounds(localBounds, float4x4.TRS(new Vector3(0, 0, 0), localSplineRotation, transform.localScale));
            Bounds transformedBounds = GeneralUtility.TransformBounds(localBounds, SplineObjectUtility.GetCombinedParentMatrixs(this));

            snapData.soStartPoint = splinePosition.z - transformedBounds.extents.z + localParentBounds.center.z;
            snapData.soEndPoint = splinePosition.z + transformedBounds.extents.z + localParentBounds.center.z;

            snapData.snapStartPoint = GetClosestPoint(snapSettings.snapMode, snapData.soStartPoint, out float startDistance);
            snapData.snapEndPoint = GetClosestPoint(snapSettings.snapMode, snapData.soEndPoint, out float endDistance);

            float midPoint = (snapData.soStartPoint + snapData.soEndPoint) / 2;

            if (midPoint > snapData.snapStartPoint && startDistance < snapSettings.startSnapDistance)
            {
                snapData.start = true;
            }

            if (midPoint < snapData.snapEndPoint && endDistance < snapSettings.endSnapDistance)
            {
                snapData.end = true;
            }

            snapData.snapStartPoint += snapSettings.startSnapOffset;
            snapData.snapEndPoint += snapSettings.endSnapOffset;

            return snapData;

            float GetClosestPoint(SnapMode snapMode, float point, out float distance)
            {
                float closestPoint = 0;
                float dCheck = 99999;

                distance = dCheck;

                if (snapMode == SnapMode.CONTROL_POINTS)
                {
                    foreach (Segment s in splineParent.segments)
                    {
                        if (s.IgnoreSnapping)
                            continue;

                        float zPoint = s.zPosition;
                        if (alignToEnd) zPoint = s.SplineParent.Length - s.zPosition;
                        float d = Mathf.Abs(zPoint - point);

                        if (dCheck > d)
                        {
                            dCheck = d;
                            distance = d;
                            closestPoint = zPoint;
                        }
                    }
                }
                else if (snapMode == SnapMode.SPLINE_POINT)
                {
                    distance = Mathf.Abs(snapSettings.snapTargetPoint - point);
                    closestPoint = snapSettings.snapTargetPoint;
                }

                return closestPoint;
            }
        }

        internal Spline TryFindSplineParent()
        {
            Transform _transform = transform;
            Spline spline = null;
            SplineObject so;

            for (int i = 0; i < 25; i++)
            {
                if (_transform.parent == null)
                    break;

                _transform = _transform.parent;
                spline = _transform.GetComponent<Spline>();
                so = _transform.GetComponent<SplineObject>();

                //Skip if parent is not spline deformation
                if (so != null && so.type != SplineObjectType.DEFORMATION)
                    break;

                if (spline != null)
                    return spline;
            }

            return spline;
        }

        private void SetRenderMesh(bool value)
        {
            if (type == SplineObjectType.DEFORMATION || type == SplineObjectType.FOLLOWER)
            {
                rendererContainer.Clear();
                gameObject.GetComponentsInChildren(rendererContainer);
                foreach (Renderer r in rendererContainer)
                    r.enabled = value;

                if (gameObject.TryGetComponent(out MeshRenderer meshRenderer))
                    meshRenderer.enabled = value;
            }

            meshRendered = value;
        }
    }
}
