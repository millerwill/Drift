// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: Segment.cs
//
// Author: Mikael Danielsson
// Date Created: 12-02-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;

using SplineArchitect.Utility;

namespace SplineArchitect
{
    [Serializable]
    public partial class Segment
    {
        internal const float defaultZRotation = 0;
        internal const float defaultContrast = 1.5f;
        internal const float defaultNoise = 1;
        internal const float defaultSaddleSkewX = 0;
        internal const float defaultSaddleSkewY = 0;
        internal const float defaultScale = 1;

        private const float contrastMin = -50f;
        private const float contrastMax = 50f;
        private const float noiseMin = 0;
        private const float noiseMax = 1;

        internal const float autoSmoothTension = 0.4f;
        internal const float autoSmoothRoundness = 1.25f;

        public const int dataUsage = 24 + 
                                     12 + 12 + 12 + 8 + 4 + 4 + 4 + 12 + 12 + 4 + 4 + 1 + 40 + 8 + 40;

        // Space
        [SerializeField, HideInInspector] private Vector3 anchor;
        [SerializeField, HideInInspector] private Vector3 tangentA;
        [SerializeField, HideInInspector] private Vector3 tangentB;
        [SerializeField, HideInInspector] private Quaternion rotation;
        [SerializeField, HideInInspector] private bool isStatic;
        [SerializeField, HideInInspector] internal Transform localSpace;

        // General
        [SerializeField, HideInInspector] internal float length;
        [SerializeField, HideInInspector] internal float zPosition;
        [SerializeField, HideInInspector] private bool ignoreSnapping;
        [SerializeField, HideInInspector] private InterpolationType interpolationType;
        [SerializeField, HideInInspector] private Vector3 oldDirTangentA = Vector3.right;
        [SerializeField, HideInInspector] private Vector3 oldDirTangentB = -Vector3.right;
        [SerializeField, HideInInspector] private float oldDisTangentA = 3;
        [SerializeField, HideInInspector] private float oldDisTangentB = 3;

        // Effects
        [SerializeField, HideInInspector] private float zRotation = defaultZRotation;
        [SerializeField, HideInInspector] private float contrast = defaultContrast;
        [SerializeField, HideInInspector] private float noise = defaultNoise;
        [SerializeField, HideInInspector] private Vector2 saddleSkew = new Vector2(defaultSaddleSkewX, defaultSaddleSkewY);
        [SerializeField, HideInInspector] private Vector2 scale = new Vector2(defaultScale, defaultScale);

        // Link related
        [HideInInspector] public bool ignoreLink;
        [HideInInspector] public Vector3 connectorPosOffset;
        [HideInInspector] public Vector3 connectorEulerOffset;
        [SerializeField, HideInInspector] internal LinkTarget linkTarget;
        [NonSerialized] internal SplineConnector splineConnector;
        [NonSerialized] internal List<Segment> links;
        [NonSerialized] private List<Segment> closestSegmentContainer;

        // General runtime
        [NonSerialized] internal Spline splineParent;
        [NonSerialized] internal int indexInSpline;

#if UNITY_EDITOR
        [NonSerialized] internal LinkTarget oldLinkTarget;
        internal bool linkCreatedThisFrameOnly { private get; set; }
#endif

        // Properties
        public float Length => length;
        public float ZPosition => zPosition;
        public LinkTarget LinkTarget => linkTarget;
        public int LinkCount => links != null ? links.Count : 0;
        public bool IsStatic
        {
            get => isStatic;
            set 
            {
                if (isStatic == value)
                    return;

                isStatic = value;

                UpdateRotation();
                UpdateLoopData();
                splineParent.MarkCacheDirty();
            }
        }
        public bool IgnoreSnapping
        {
            get => ignoreSnapping;
            set
            {
                if (value == ignoreSnapping)
                    return;

                ignoreSnapping = value;
                splineParent.MarkCacheDirty();
            }
        }
        public float TangentADistance
        {
            get => Vector3.Distance(anchor, tangentA);
        }
        public float TangentBDistance
        {
            get => Vector3.Distance(anchor, tangentB);
        }
        public Quaternion Rotation
        {
            get => localSpace.rotation * rotation;
            set
            {
                LocalRotation = Quaternion.Inverse(localSpace.rotation) * value;
            }
        }
        public Quaternion LocalRotation
        {
            get => rotation;
            set
            {
#if UNITY_EDITOR
                if (isStatic)
                {
                    Debug.LogWarning("[Spline Architect] You can't set the rotation on static segments.");
                    return;
                }
#endif

                if (GeneralUtility.IsEqualFast(value, rotation))
                    return;

                Quaternion delta = value * Quaternion.Inverse(rotation);
                tangentA = anchor + delta * (tangentA - anchor);
                tangentB = anchor + delta * (tangentB - anchor);
                rotation = value;

                UpdateLoopData();
                splineParent.MarkCacheDirty();
            }
        }
        public float ZRotation
        {
            get => zRotation;
            set 
            {
                if (GeneralUtility.IsEqual(value, zRotation))
                    return;

                float dif = ZRotation - value;
                zRotation = value;

                UpdateLoopData();
                splineParent.MarkCacheDirty();
            }
        }
        public float Contrast
        {
            get => contrast;
            set
            {
                if (GeneralUtility.IsEqual(value, contrast))
                    return;

                contrast = Mathf.Clamp(value, contrastMin, contrastMax);
                UpdateLoopData();
                splineParent.MarkCacheDirty();
            }
        }
        public float Noise
        {
            get => noise;
            set 
            {
                if (GeneralUtility.IsEqual(value, noise))
                    return;

                noise = Mathf.Clamp(value, noiseMin, noiseMax);
                UpdateLoopData();
                splineParent.MarkCacheDirty();
            }
        }
        public Vector2 SaddleSkew
        {
            get => saddleSkew;
            set
            {
                if (GeneralUtility.IsEqual(value, saddleSkew))
                    return;

                saddleSkew = value;
                UpdateLoopData();
                splineParent.MarkCacheDirty();
            }
        }
        public Vector2 Scale
        {
            get => scale;
            set
            {
                if (GeneralUtility.IsEqual(value, scale))
                    return;

                scale = value;
                UpdateLoopData();
                splineParent.MarkCacheDirty();
            }
        }
        public int IndexInSpline => indexInSpline;
        public Spline SplineParent => splineParent;
        public SplineConnector SplineConnector => splineConnector;

        public Segment(Spline splineParent, Vector3 position, float distanceTangentA, float distanceTangentB, Quaternion rotation, Space space, int indexInSpline = -1)
        {
            localSpace = splineParent.transform;
            this.splineParent = splineParent;
            isStatic = false;

            if (space == Space.World) this.rotation = Quaternion.Inverse(localSpace.rotation) * rotation;
            else this.rotation = rotation;
            SetPosition(ControlHandle.ANCHOR, position, space);
            SetPosition(ControlHandle.TANGENT_A, position + (rotation * Vector3.forward) * distanceTangentA, space);
            SetPosition(ControlHandle.TANGENT_B, position + (rotation * -Vector3.forward) * distanceTangentB, space);

            if (indexInSpline < 0) this.indexInSpline = GetIndexInSpline();
            else this.indexInSpline = indexInSpline;
        }

        public Segment(Spline splineParent, Vector3 position, Vector3 tangentA, Vector3 tangentB, Space space, int indexInSpline = -1)
        {
            localSpace = splineParent.transform;
            this.splineParent = splineParent;
            isStatic = true;

            SetPosition(ControlHandle.ANCHOR, position, space);
            SetPosition(ControlHandle.TANGENT_A, tangentA, space);
            SetPosition(ControlHandle.TANGENT_B, tangentB, space);

            if (indexInSpline < 0) this.indexInSpline = GetIndexInSpline();
            else this.indexInSpline = indexInSpline;
        }

        public Vector3 GetPosition(ControlHandle controlHandle, Space space = Space.World)
        {
            if(space == Space.Self)
                return GetLocalPosition(controlHandle);

            return localSpace.TransformPoint(GetLocalPosition(controlHandle));
        }

        public Vector3 GetDirection(Space space = Space.World)
        {
            return (GetPosition(ControlHandle.TANGENT_A, space) - GetPosition(ControlHandle.ANCHOR, space)).normalized;
        }

        public void Rotate(Vector3 eulerAngles, Space space = Space.Self)
        {
            Quaternion delta = Quaternion.Euler(eulerAngles);
            if (space == Space.World) Rotation = delta * Rotation;
            else LocalRotation = LocalRotation * delta;
        }

        public void SetPosition(ControlHandle controlHandle, Vector3 newPosition, 
                                                             Space space = Space.World)
        {
            if(space == Space.Self)
                SetLocalPosition(controlHandle, newPosition);
            else
                SetLocalPosition(controlHandle, localSpace.InverseTransformPoint(newPosition));
        }

        public void Translate(ControlHandle controlHandle, Vector3 value, 
                                                           Space space = Space.World)
        {
            SetPosition(controlHandle, GetPosition(controlHandle, space) - value, space);
        }

        public void SetAnchorPosition(Vector3 newPosition, Space space = Space.World)
        {
            Vector3 oldPosition = GetPosition(ControlHandle.ANCHOR, space);
            SetPosition(ControlHandle.ANCHOR, newPosition, space);
            Translate(ControlHandle.TANGENT_A, oldPosition - newPosition, space);
            Translate(ControlHandle.TANGENT_B, oldPosition - newPosition, space);

            UpdateLoopPosition(space);
        }

        public void TranslateAnchor(Vector3 value, Space space = Space.World)
        {
            Translate(ControlHandle.ANCHOR, value, space);
            Translate(ControlHandle.TANGENT_A, value, space);
            Translate(ControlHandle.TANGENT_B, value, space);

            UpdateLoopPosition(space);
        }

        /// <summary>
        /// Moves the control handle and preserves the direction of the opposite tangent, 
        /// but not its length. Moving the anchor translates both tangents along with it.
        /// </summary>
        public void SetContinuousPosition(ControlHandle controlHandle, 
                                          Vector3 newPosition, 
                                          Space space = Space.World)
        {
            if(controlHandle == ControlHandle.ANCHOR)
            {
                SetAnchorPosition(newPosition, space);
            }
            else if (controlHandle == ControlHandle.TANGENT_A || controlHandle == ControlHandle.TANGENT_B)
            {
                ControlHandle opositeType = SplineUtility.GetOppositeTangentType(controlHandle);
                float distance = Vector3.Distance(GetPosition(opositeType, space), GetPosition(ControlHandle.ANCHOR, space));
                Vector3 direction = (newPosition - GetPosition(ControlHandle.ANCHOR, space)).normalized;
                Vector3 oppositePosition = GetPosition(ControlHandle.ANCHOR, space) - direction * distance;

                if(!GeneralUtility.IsEqual(newPosition, GetPosition(ControlHandle.ANCHOR, space)))
                    SetPosition(opositeType, oppositePosition, space);

                SetPosition(controlHandle, newPosition, space);
            }

            UpdateLoopPosition(space);
        }

        /// <summary>
        /// Moves the control handle and mirrors the opposite tangent. 
        /// Moving the anchor also translates both tangents along with it.
        /// </summary>
        public void SetMirroredPosition(ControlHandle controlHandle, 
                                        Vector3 newPosition, 
                                        Space space = Space.World)
        {
            if (controlHandle == ControlHandle.ANCHOR)
            {
                SetAnchorPosition(newPosition, space);
            }
            else if (controlHandle == ControlHandle.TANGENT_A || controlHandle == ControlHandle.TANGENT_B)
            {
                float distance = Vector3.Distance(newPosition, GetPosition(ControlHandle.ANCHOR, space));

                Vector3 direction = (newPosition - GetPosition(ControlHandle.ANCHOR, space)).normalized;
                SetPosition(SplineUtility.GetOppositeTangentType(controlHandle), GetPosition(ControlHandle.ANCHOR) - direction * distance, space);
                SetPosition(controlHandle, newPosition, space);
            }

            UpdateLoopPosition(space);
        }

        /// <summary>
        /// Moving a tangent does not affect the opposite tangent. 
        /// Moving the anchor translates both tangents along with it.
        /// </summary>
        public void SetBrokenPosition(ControlHandle controlHandle, 
                                      Vector3 newPosition, 
                                      Space space = Space.World)
        {
            if (controlHandle == ControlHandle.ANCHOR)
            {
                SetAnchorPosition(newPosition, space);
            }
            else if (controlHandle == ControlHandle.TANGENT_A || controlHandle == ControlHandle.TANGENT_B)
            {
                SetPosition(controlHandle, newPosition, space);
                UpdateLoopPosition(space);
            }
        }

        public void SetTangentDirection(Vector3 direction)
        {
            float disA = TangentADistance;
            float disB = TangentBDistance;
            Vector3 anchor = GetPosition(ControlHandle.ANCHOR);

            SetPosition(ControlHandle.TANGENT_A, anchor + direction * disA);
            SetPosition(ControlHandle.TANGENT_B, anchor - direction * disB);
        }

        public void ApplyAutoSmooth()
        {
            Segment prevSegment = PrevSegment();
            Segment nextSegment = NextSegment();
            Vector3 prevAnchor = Vector3.zero;
            Vector3 nextAnchor = Vector3.zero;
            bool usePrev = false;
            bool useNext = false;

            if (prevSegment != null)
            {
                prevAnchor = prevSegment.GetPosition(ControlHandle.ANCHOR, Space.Self);
                usePrev = true;
            }

            if (nextSegment != null)
            {
                nextAnchor = nextSegment.GetPosition(ControlHandle.ANCHOR, Space.Self);
                useNext = true;
            }

            ApplyAutoSmooth(prevAnchor, usePrev, nextAnchor, useNext);
            UpdateLoopPosition();

            if(prevSegment != null && prevSegment.indexInSpline == 0 && prevSegment.splineParent.Loop)
                prevSegment.UpdateLoopPosition();

            if (nextSegment != null && nextSegment.indexInSpline == 0 && nextSegment.splineParent.Loop)
                nextSegment.UpdateLoopPosition();
        }

        public void ApplyAutoSmooth(Vector3 prevAnchorPos, bool usePrev, Vector3 nextAnchorPos, bool useNext)
        {
            NativeSegment nativeSegment = new NativeSegment(anchor, tangentA, tangentB, rotation, isStatic);
            nativeSegment = SplineUtility.CalculateAutoSmoothSegment(autoSmoothTension, autoSmoothRoundness, nativeSegment, prevAnchorPos, usePrev, nextAnchorPos, useNext, indexInSpline == 0);

            if (isStatic)
                SetTangentDirection((Quaternion)nativeSegment.rotation * Vector3.forward);
            else
                LocalRotation = nativeSegment.rotation;

            SetPosition(ControlHandle.TANGENT_A, nativeSegment.tangentA, Space.Self);
            SetPosition(ControlHandle.TANGENT_B, nativeSegment.tangentB, Space.Self);

            if(indexInSpline == 0 && splineParent.Loop)
            {
                splineParent.segments[splineParent.segments.Count - 1].ApplyAutoSmooth();
            }
        }

        /// <summary>
        /// Sets the interpolation type of the segment, 
        /// switching between a smooth spline curve and a straight line,
        /// and updates the tangents accordingly.
        /// </summary>
        public void SetInterpolationType(InterpolationType interpolationType)
        {
            if (splineParent == null)
                return;

            if (interpolationType == this.interpolationType)
                return;

            if (this.interpolationType == InterpolationType.SPLINE && interpolationType == InterpolationType.LINE)
            {
                oldDisTangentA = Vector3.Distance(anchor, tangentA);
                oldDisTangentB = Vector3.Distance(anchor, tangentB);
                oldDirTangentA = (tangentA - anchor).normalized;
                oldDirTangentB = (tangentB - anchor).normalized;
            }

            this.interpolationType = interpolationType;

            if (splineParent.segments.Count == 1)
                return;

            if (interpolationType == InterpolationType.SPLINE)
            {
                if (GeneralUtility.IsZero(oldDirTangentA) || GeneralUtility.IsZero(oldDirTangentB) || oldDisTangentA < 0.5f || oldDisTangentB < 0.5f)
                {
                    tangentA = anchor + (rotation * Vector3.forward) * 3f;
                    tangentB = anchor - (rotation * Vector3.forward) * 3f;
                }
                else
                {
                    tangentA = anchor + oldDirTangentA * oldDisTangentA;
                    tangentB = anchor + oldDirTangentB * oldDisTangentB;
                }
            }

            splineParent.MarkCacheDirty();
        }

        /// <summary>
        /// Gets the current interpolation type of the segment (Spline or Line).
        /// </summary>
        public InterpolationType GetInterpolationType()
        {
            return interpolationType;
        }

        public Segment NextSegment()
        {
            int nextIndex = splineParent.Loop && indexInSpline == splineParent.segments.Count - 2 ? 
                indexInSpline + 2 : indexInSpline + 1;

            if (nextIndex >= splineParent.segments.Count)
            {
                nextIndex = 0;

                if (!splineParent.Loop)
                    return null;
            }
            return splineParent.segments[nextIndex];
        }

        public Segment PrevSegment()
        {
            int prevIndex = indexInSpline - 1;
            if (prevIndex < 0)
            {
                prevIndex = splineParent.segments.Count - 2;

                if (!splineParent.Loop)
                    return null;
            }

            return splineParent.segments[prevIndex];
        }

        public Segment GetLinkAtIndex(int index)
        {
            return links[index];
        }

        /// <summary>
        /// Manually links this segment to another segment at their anchor positions.
        /// </summary>
        public void AddSegmentLink(Segment segment)
        {
            if(linkTarget == LinkTarget.SPLINE_CONNECTOR || segment.linkTarget == LinkTarget.SPLINE_CONNECTOR)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[Spline Architect] Cannot add a link to a segment that is already connected to a spline connector.");
#endif
                return;
            }

            if (links == null)
                links = new List<Segment> { this };

            if (segment.links == null)
                segment.links = new List<Segment> { segment };

            //Set external segment data
            Vector3 anchorPoint = GetPosition(ControlHandle.ANCHOR);
            segment.SetAnchorPosition(anchorPoint);
            if (!segment.links.Contains(this)) segment.links.Add(this);
#if UNITY_EDITOR
            else Debug.LogWarning($"[Spline Architect] Segment {segment.indexInSpline} is already linked with {indexInSpline} and cannot be linked again.");
#endif
            segment.linkTarget = LinkTarget.ANCHOR;

            //Set this segment data
            if (!links.Contains(segment)) links.Add(segment);
            linkTarget = LinkTarget.ANCHOR;
        }

        /// <summary>
        /// Removes and unlinks a specific segment that is currently linked to this segment.
        /// </summary>
        public void RemoveSegmentLink(Segment segment)
        {
            if (LinkCount == 0)
                return;

            if (!links.Contains(segment))
                return;

            links.Remove(segment);
            if(LinkCount == 1)
            {
                links.Clear();
                linkTarget = LinkTarget.NONE;
            }

            if (segment.LinkCount == 0)
                return;

            if (!segment.links.Contains(this))
                return;

            segment.links.Remove(this);
            if (segment.LinkCount == 1)
            {
                segment.links.Clear();
                segment.linkTarget = LinkTarget.NONE;
            }
        }

        public bool ContainsLink(Segment segment)
        {
            if (LinkCount == 0)
                return false;

            return links.Contains(segment);
        }

        /// <summary>
        /// Moves and links this segment to a specific position 
        /// and connects it with all other segments at the same point.
        /// If forceLinkUnlinked is true, segments at this point 
        /// with LinkTarget = None will also be linked.
        /// </summary>
        public bool LinkToSegment(Vector3 point, bool forceLinkUnlinked = true)
        {
            SetAnchorPosition(point);
            splineParent.CalculateControlpointBounds();

            if (closestSegmentContainer == null)
                closestSegmentContainer = new();

#if UNITY_EDITOR
            HashSet<Spline> splines = null;

            if((linkCreatedThisFrameOnly && !EHandleEvents.undoActive) ||
                linkCreatedThisFrameOnly && EHandleEvents.undoActive && EHandleEvents.undoWasRedo) splines = HandleRegistry.GetSplinesRegistredThisFrame();
            else splines = HandleRegistry.GetSplinesUnsafe();

            SplineUtility.GetSegmentsAtPointNoAlloc(closestSegmentContainer, splines, point);
#else
            SplineUtility.GetSegmentsAtPointNoAlloc(closestSegmentContainer, HandleRegistry.GetSplinesUnsafe(), point);
#endif

            bool sucessfullLink = closestSegmentContainer.Count > 1;
            if (sucessfullLink)
            {
                linkTarget = LinkTarget.ANCHOR;

#if UNITY_EDITOR
                if (!EHandleEvents.controlPointCreationActive)
                    oldLinkTarget = LinkTarget.ANCHOR;
#endif
            }

            //Update links
            foreach (Segment s in closestSegmentContainer)
            {
                if (!forceLinkUnlinked && s.linkTarget != LinkTarget.ANCHOR)
                    continue;

                if (s.links == null)
                    s.links = new List<Segment>();

                s.links.Clear();
                s.linkTarget = LinkTarget.ANCHOR;

                foreach (Segment s2 in closestSegmentContainer)
                {
                    if (!forceLinkUnlinked && s2.linkTarget != LinkTarget.ANCHOR)
                        continue;

                    //Add segment to forceLinkUnlinked
                    s.links.Add(s2);
                }
            }

            return sucessfullLink;
        }

        /// <summary>
        /// Moves this segment to the specified position 
        /// and links it to the first SplineConnector found at that point.
        /// </summary>
        public bool LinkToConnector(Vector3 point)
        {
            SetAnchorPosition(point);

#if UNITY_EDITOR
            HashSet<SplineConnector> connectors = HandleRegistry.GetSplineConnectorsUnsafe();
            HashSet<SplineConnector> thisFrameConnectors = HandleRegistry.GetSplineConnectorsRegistredThisFrame();
            if (thisFrameConnectors.Count > 0 || (linkCreatedThisFrameOnly && !EHandleEvents.undoActive)) connectors = thisFrameConnectors;
            SplineConnector closest = SplineConnectorUtility.GetFirstConnectorAtPoint(point, connectors);
#else
            SplineConnector closest = SplineConnectorUtility.GetClosest(point, HandleRegistry.GetSplineConnectorsUnsafe());
#endif
            bool sucessfullLink = closest != null;

            if (sucessfullLink)
            {
                closest.AddConnection(this);
                splineConnector = closest;
                linkTarget = LinkTarget.SPLINE_CONNECTOR;
#if UNITY_EDITOR
                if (!EHandleEvents.controlPointCreationActive) 
                    oldLinkTarget = LinkTarget.SPLINE_CONNECTOR;
#endif
            }

            return sucessfullLink;
        }

        /// <summary>
        /// Unlinks this segment from any other segment or spline connector.
        /// </summary>
        public void Unlink()
        {
            linkTarget = LinkTarget.NONE;

            if (splineConnector != null)
            {
                splineConnector.RemoveConnection(this);
                splineConnector = null;
            }

            if (links == null)
                return;

            for (int i2 = 0; i2 < LinkCount; i2++)
            {
                Segment s = links[i2];

                //Skip self
                if (s == this)
                    continue;

                if (s.LinkCount <= 2)
                {
                    s.links.Clear();
                    s.linkTarget = LinkTarget.NONE;
                }
                else
                {
                    for (int i = 0; i < s.LinkCount; i++)
                    {
                        Segment s2 = s.links[i];

                        if (s2 == this)
                        {
                            s.links.RemoveAt(i);
                            break;
                        }
                    }
                }
            }

            links.Clear();
        }

        public void MoveToSurface()
        {
            if (Physics.Raycast(GetPosition(ControlHandle.ANCHOR), Vector3.down, out RaycastHit hit))
            {
                SetAnchorPosition(hit.point);

                Vector3 forward = Rotation * Vector3.forward;
                Vector3 up = hit.normal;
                forward = Vector3.ProjectOnPlane(forward, up).normalized;

                if (isStatic)
                    SetTangentDirection(forward);
                else
                {
                    if (forward.sqrMagnitude > 0.000001f)
                        Rotation = Quaternion.LookRotation(forward, up);
                }
            }
        }

        internal void UpdateLineOrientation()
        {
            if (interpolationType != InterpolationType.LINE)
                return;

            if (splineParent.SegmentCount < 2)
                return;

            Vector3 direction;
            if (indexInSpline < splineParent.segments.Count - 1)
            {
                Segment next = splineParent.segments[indexInSpline + 1];
                direction = (anchor - next.tangentB).normalized;
            }
            else
            {
                Segment prev = splineParent.segments[indexInSpline - 1];
                direction = (prev.tangentA - anchor).normalized;
            }

            tangentA = anchor - direction * 0.1f;
            tangentB = anchor + direction * 0.1f;

            UpdateRotation();
        }

        internal void SwitchSplineParent(Spline splineParent, int indexInSpline = -1)
        {
            Vector3 anchor = GetPosition(ControlHandle.ANCHOR);
            Vector3 tangentA = GetPosition(ControlHandle.TANGENT_A);
            Vector3 tangentB = GetPosition(ControlHandle.TANGENT_B);

            Spline oldSplineParent = this.splineParent;
            this.splineParent = splineParent;
            localSpace = splineParent.transform;

            if (indexInSpline < 0)
                this.indexInSpline = GetIndexInSpline();
            else
                this.indexInSpline = indexInSpline;

            if (oldSplineParent != null && splineParent != oldSplineParent)
            {
                SetPosition(ControlHandle.ANCHOR, anchor);
                SetPosition(ControlHandle.TANGENT_A, tangentA);
                SetPosition(ControlHandle.TANGENT_B, tangentB);
            }
        }

        internal void UpdateRotation(bool forceStatic = false)
        {
            Vector3 dir = GetDirection(Space.Self);
            if (dir.sqrMagnitude > 0.0001f)
            {
                if (IsStatic || forceStatic)
                {
                    Vector3 xNormal = Vector3.Cross(dir, -Vector3.up).normalized;
                    Vector3 yNormal = Vector3.Cross(dir, xNormal).normalized;
                    rotation = Quaternion.LookRotation(dir, yNormal);
                }
                else
                {
                    rotation = Quaternion.LookRotation(dir, rotation * Vector3.up);
                }
            }
        }

        private Vector3 GetLocalPosition(ControlHandle controlHandle)
        {
            if (controlHandle == ControlHandle.TANGENT_A)
                return tangentA;
            else if (controlHandle == ControlHandle.TANGENT_B)
                return tangentB;
            else
                return anchor;
        }

        private void SetLocalPosition(ControlHandle controlHandle, Vector3 newPosition)
        {
            if (controlHandle == ControlHandle.TANGENT_A)
            {
                if (GeneralUtility.IsEqual(newPosition, tangentA))
                    return;

                tangentA = newPosition;
                UpdateRotation();
            }
            else if (controlHandle == ControlHandle.TANGENT_B)
            {
                if (GeneralUtility.IsEqual(newPosition, tangentB))
                    return;

                tangentB = newPosition;
            }
            else
            {
                if (GeneralUtility.IsEqual(newPosition, anchor))
                    return;

                anchor = newPosition;
            }

#if UNITY_EDITOR
            if (splineParent != null)
                splineParent.MarkCacheDirty();
            else
            {
                splineParent = localSpace.GetComponent<Spline>();
                if (splineParent != null)
                    splineParent.MarkCacheDirty();
            }
#else
            splineParent.MarkCacheDirty();
#endif
        }

        private void UpdateLoopPosition(Space space = Space.World)
        {
            if(splineParent == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[Spline Architect] splineParent is null!");
#endif
                return;
            }

            if (!splineParent.Loop || this != splineParent.segments[0])
                return;

            splineParent.segments[splineParent.segments.Count - 1].SetPosition(ControlHandle.ANCHOR, GetPosition(ControlHandle.ANCHOR, space), space);
            splineParent.segments[splineParent.segments.Count - 1].SetPosition(ControlHandle.TANGENT_A, GetPosition(ControlHandle.TANGENT_A, space), space);
            splineParent.segments[splineParent.segments.Count - 1].SetPosition(ControlHandle.TANGENT_B, GetPosition(ControlHandle.TANGENT_B, space), space);
        }

        private void UpdateLoopData()
        {
            if (splineParent == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[Spline Architect] splineParent is null!");
#endif
                return;
            }

            if (!splineParent.Loop || indexInSpline != 0)
                return;

            Segment last = splineParent.segments[splineParent.segments.Count - 1];

            last.rotation = rotation;
            last.contrast = contrast;
            last.scale = scale;
            last.noise = noise;
            last.saddleSkew = saddleSkew;
            last.zRotation = zRotation;
        }

        private int GetIndexInSpline()
        {
            int index = -1;

            for (int i = 0; i < splineParent.segments.Count; i++)
            {
                if (splineParent.segments[i] == this)
                {  
                    index = i; 
                    break; 
                }
            }

            return index;
        }
    }
}