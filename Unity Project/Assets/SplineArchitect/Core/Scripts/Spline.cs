// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: Spline.cs
//
// Author: Mikael Danielsson
// Date Created: 25-03-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System;

using Unity.Collections;
using UnityEngine.Rendering;
using UnityEngine;
using Unity.Mathematics;

using SplineArchitect.Utility;
using SplineArchitect.Monitor;
using SplineArchitect.Workers;

using Vector3 = UnityEngine.Vector3;
using LineUtility = SplineArchitect.Utility.LineUtility;

namespace SplineArchitect
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("SplineArchitect/Spline")]
    public partial class Spline : MonoBehaviour
    {
        public const int dataUsage = 32 +
                                     1 + 4 + 1 + 40 + 40 + 1 + 1 + 4 + 1 + 1 + MonitorSpline.dataUsage + 40 + 72 +
                                     1 + 1 + 1 + 1 + 1 + 24 + 24 + 32 + 32 + 32 + 32 + 32 + 4 + 4 + 4 + 4 + 4 + 4;

        // Stored
        [HideInInspector, SerializeField] private float length;
        [HideInInspector, SerializeField] private bool loop;
        [HideInInspector, SerializeField] private bool renderInGame;
        [HideInInspector, SerializeField] private float width = 2;
        [HideInInspector, SerializeField] private float distanceScale = 100;
        [HideInInspector, SerializeField] private bool occluded;
        [HideInInspector, SerializeField] internal List<Segment> segments;
        [HideInInspector, SerializeField] internal SplineType splineType = SplineType.NOT_USED;
        [HideInInspector, SerializeField] internal ComponentMode componentMode = ComponentMode.NONE;

        // Runtime
        [NonSerialized] private MonitorSpline monitor;
        [NonSerialized] private List<SplineObject> allSplineObjects = new List<SplineObject>();
        [NonSerialized] private List<SplineObject> rootSplineObjects = new List<SplineObject>();
        [NonSerialized] private HashSet<SplineObject> splineObjectsSet = new HashSet<SplineObject>();
        [NonSerialized] private Vector3[] normalsContainer = new Vector3[3];
        [NonSerialized] private List<Transform> transformContainer = new List<Transform>();
        [NonSerialized] private AnimationCurve lineWidthCurve = null;
        [NonSerialized] private Vector3 oldRenderCameraPos = new Vector3(99999, 99999, 99999);
        [NonSerialized] private int frameCounter;
        [NonSerialized] internal List<(SplineObject, int)> detachList = new List<(SplineObject, int)>();
        [NonSerialized] internal List<SplineObject> attachList = new List<SplineObject>();

        // Values
        /// <summary>
        /// A list of noise layers that can be added or removed to affect spline deformation.
        /// The spline will automatically detect changes to this list and update accordingly.
        /// </summary>
        [HideInInspector] public List<NoiseLayer> noises;
        /// <summary>
        /// Set what camera the spline line should use for different calculations.
        /// </summary>
        [HideInInspector] public Camera lineRenderCamera;
        [HideInInspector] public Color color = new Color(0, 0, 0, 1);
        [HideInInspector] public NoiseGroup noiseGroup = NoiseGroup.A;
        [HideInInspector] public AnimationCurve scaleProfileCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f));

        // Properties
        internal MonitorSpline Monitor => monitor;
        /// <summary>
        /// Indicates whether the spline is looping. Use spline.SetLoop() to enable or disable looping.
        /// </summary>
        public bool Loop => loop;
        /// <summary>
        /// The width of the rendered spline in the editor and in game (when RenderInGame is true).
        /// </summary>
        public float Width
        {
            get => width;
            set
            {
                if (GeneralUtility.IsEqual(width, value))
                    return;

                if (width <= 0)
                {
#if UNITY_EDITOR
                    Debug.LogWarning("[SplineArchitect] Render width can't be zero or less then zero!");
#endif
                    width = 0.001f;
                }

                width = value;
                RebuildSplineLine(true);
            }
        }
        /// <summary>
        /// The distance scale used when rendering the spline in the editor and in game (when RenderInGame is true).
        /// </summary>
        public float DistanceScale
        {
            get => distanceScale;
            set
            {
                if (GeneralUtility.IsEqual(distanceScale, value))
                    return;

                if (distanceScale < 0.999f)
                {
#if UNITY_EDITOR
                    Debug.LogWarning("[SplineArchitect] Render distance scale can't be less then one!");
#endif
                    distanceScale = 1;
                }

                distanceScale = value;
                RebuildSplineLine(true);
            }
        }
        /// <summary>
        /// Controls whether the spline is occluded in the editor and in game (when RenderInGame is true).
        /// </summary>
        public bool Occluded
        {
            get => occluded;
            set
            {
                if (occluded == value)
                    return;

                if (renderMaterial != null)
                {
                    if (value) renderMaterial.SetInt("_ZTest", (int)CompareFunction.LessEqual);
                    else renderMaterial.SetInt("_ZTest", (int)CompareFunction.Always);
                }

                occluded = value;
                RebuildSplineLine(true);
            }
        }
        /// <summary>
        /// The LineRenderer component used for drawing the spline in Play Mode and in the built game (when RenderInGame is true).
        /// </summary>
        public LineRenderer lineRenderer { get; private set; } = null;
        /// <summary>
        /// The material the LineRenderer uses for drawing the spline in Play Mode and in the built game (when RenderInGame is true).
        /// </summary>
        public Material renderMaterial { get; private set; } = null;
        public float Length => length;
        public bool RenderInGame
        {
            get => renderInGame;
            set
            {
                if (renderInGame == value)
                    return;

                renderInGame = value;
                RebuildSplineLine(true);
            }
        }
        public ComponentMode ComponentMode
        {
            get => componentMode;
            set => componentMode = value;
        }
        public int SegmentCount => segments != null ? segments.Count : 0;
        public int AllSplineObjectCount => allSplineObjects != null ? allSplineObjects.Count : 0;
        public int RootSplineObjectCount => rootSplineObjects != null ? rootSplineObjects.Count : 0;

        public Segment GetSegmentAtIndex(int index)
        {
            return segments[index];
        }

        public void AddSegment(Segment segment)
        {
            segment.SwitchSplineParent(this);
            segments.Add(segment);
            MarkCacheDirty();
        }

        public void InsertSegment(int index, Segment segment)
        {
            segment.SwitchSplineParent(this, index);
            segments.Insert(index, segment);
            MarkCacheDirty();
        }

        public Segment CreateSegment(Vector3 anchorPos,
                                    float tangentADis,
                                    float tangentBDis,
                                    Space space = Space.World)
        {
            return CreateSegment(segments.Count, anchorPos, tangentADis, tangentBDis, segments[SegmentCount - 1].Rotation, space);
        }

        public Segment CreateSegment(Vector3 anchorPos,
                                     float tangentADis,
                                     float tangentBDis, 
                                     Quaternion rotation, Space space = Space.World)
        {

            return CreateSegment(segments.Count, anchorPos, tangentADis, tangentBDis, rotation, space);
        }

        public Segment CreateSegment(int index, Vector3 anchorPos,
                                                float tangentADis,
                                                float tangentBDis,
                                                Quaternion rotation, Space space = Space.World)
        {
            Segment segment = new Segment(this, anchorPos, tangentADis, tangentBDis, rotation, space, index);
            segments.Insert(index, segment);

#if UNITY_EDITOR
            CopySettingsFromClosestSegment(segment, index);
            EHandleEvents.InvokeAfterSegmentCreated(segment);
#endif
            return segment;
        }

        /// <summary>
        /// Creates a new static segment. Static segments use the spline transform's up direction
        /// as the base rotation when normals are created. No cached normals are needed, which can
        /// improve performance. However, static segments will not work well with loops or similar shapes.
        /// </summary>
        public Segment CreateStaticSegment(Vector3 anchorPos,
                                         Vector3 tangentAPos,
                                         Vector3 tangentBPos,
                                         Space space = Space.World)
        {
            return CreateStaticSegment(segments.Count, anchorPos, tangentAPos, tangentBPos, space);
        }

        public Segment CreateStaticSegment(int index, Vector3 anchorPos,
                                                      Vector3 tangentAPos,
                                                      Vector3 tangentBPos,
                                                      Space space = Space.World)
        {
            Segment segment = new Segment(this, anchorPos, tangentAPos, tangentBPos, space, index);
            segments.Insert(index, segment);

#if UNITY_EDITOR
            CopySettingsFromClosestSegment(segment, index);
            EHandleEvents.InvokeAfterSegmentCreated(segment);
#endif
            return segment;
        }

        /// <summary>
        /// Creates a new segment at the specified time without changing the spline's shape.
        /// </summary>
        public Segment CreateSegmentAutoSmooth(float time)
        {
            AutoSmoothData autoSmoothData = SplineUtility.ComputeAutoSmoothData(NativeSegmentsLocal, time);
            int index = autoSmoothData.index;
            Vector3 newAnchor = autoSmoothData.anchor;
            Vector3 newTangentA = autoSmoothData.tangentA;
            Vector3 newTangentB = autoSmoothData.tangentB;
            Vector3 prevTangentA = autoSmoothData.prevTangentA;
            Vector3 nextTangentB = autoSmoothData.nextTangentB;

            segments[index - 1].SetPosition(ControlHandle.TANGENT_A, prevTangentA, Space.Self);
            segments[index].SetPosition(ControlHandle.TANGENT_B, nextTangentB, Space.Self);
            if (loop && index == segments.Count - 1) segments[0].SetPosition(ControlHandle.TANGENT_B, nextTangentB, Space.Self);

            float tADis = Vector3.Distance(newAnchor, newTangentA);
            float tBDis = Vector3.Distance(newAnchor, newTangentB);

            GetNormalNonAlloc(normalsContainer, time, Space.Self);
            Quaternion rotation = Quaternion.LookRotation(normalsContainer[2], normalsContainer[1]);

            Segment segment = CreateSegment(index, newAnchor, tADis, tBDis, rotation, Space.Self);
            return segment;
        }

        public bool ContainsSegment(Segment segment)
        {
            if (segments == null)
                return false;

            return segments.Contains(segment);
        }

        public void RemoveSegment(Segment segment)
        {
            segments.Remove(segment);
            MarkCacheDirty();
        }

        public void RemoveSegmentAt(int index)
        {
            segments.RemoveAt(index);
            MarkCacheDirty();
        }

        public void ReverseSegments()
        {
            segments.Reverse();
            foreach (Segment s in segments)
            {
                Vector3 tangetA = s.GetPosition(ControlHandle.TANGENT_A);
                Vector3 tangetB = s.GetPosition(ControlHandle.TANGENT_B);
                s.SetPosition(ControlHandle.TANGENT_A, tangetB);
                s.SetPosition(ControlHandle.TANGENT_B, tangetA);
            }

            MarkCacheDirty();
        }

        public int GetSegment(float time)
        {
            return SplineUtility.GetSegmentIndex(segments.Count, time);
        }

        public SplineObject GetSplineObjectAtIndex(int index)
        {
            if (index >= AllSplineObjectCount)
                return null;

            return allSplineObjects[index];
        }

        public SplineObject GetRootSplineObjectAtIndex(int index)
        {
            return rootSplineObjects[index];
        }

        public bool ContainsSplineObject(SplineObject so)
        {
            if (so == null) return false;
            return splineObjectsSet.Contains(so);
        }

        internal bool IsEnabled()
        {
            //Yes, this can happen
            if (this == null)
                return false;

            if (gameObject != null && gameObject.activeInHierarchy && enabled)
                return true;

            return false;
        }

        // If you are looking for a way to create spline objects, you should not use this function.
        // Use spline.CreateDeformation() or spline.CreateFollower().
        internal void AddSplineObject(SplineObject so, int index = -1)
        {
            if (so == null)
                return;

            if (splineObjectsSet.Contains(so))
                return;

            splineObjectsSet.Add(so);
            if (so.transform.parent == transform)
            {
                rootSplineObjects.Add(so);

                if (so.spreadFrame == -1)
                    so.spreadFrame = AllSplineObjectCount;
            }
            else if(so.spreadFrame == -1)
            {
                SplineObject soParent = SplineObjectUtility.GetRootSplineObject(so);

                if (soParent.spreadFrame == -1)
                    soParent.spreadFrame = AllSplineObjectCount;

                so.spreadFrame = soParent.spreadFrame;
            }

            if (index == -1)
                allSplineObjects.Add(so);
            else
                allSplineObjects.Insert(index, so);

#if UNITY_EDITOR
            EHandleEvents.ForceUpdateSelection();
            EHandleEvents.MarkForInfoUpdate(this);
#endif
        }

        // If you are looking for a way to remove spline objects from the spline, you should not use this function.
        // Instead, use: splineObject.transform.parent = null
        // After that, the spline object is removed from the spline and you can pool or destroy it.
        internal void RemoveSplineObject(SplineObject so)
        {
            if (!splineObjectsSet.Contains(so))
                return;

            splineObjectsSet.Remove(so);
            allSplineObjects.Remove(so);
            rootSplineObjects.Remove(so);

#if UNITY_EDITOR
            EHandleEvents.ForceUpdateSelection();
            EHandleEvents.MarkForInfoUpdate(this);
#endif
        }

        // If you are looking for a way to remove spline objects from the spline, you should not use this function.
        // Instead, use: splineObject.transform.parent = null
        // After that, the spline object is removed from the spline and you can pool or destroy it.
        internal void RemoveAtSplineObject(int index)
        {
            SplineObject so = allSplineObjects[index];

            splineObjectsSet.Remove(so);
            rootSplineObjects.Remove(so);
            allSplineObjects.RemoveAt(index);

#if UNITY_EDITOR
            EHandleEvents.ForceUpdateSelection();
            EHandleEvents.MarkForInfoUpdate(this);
#endif
        }

        /// <summary>
        /// Gets the position on a specific segment of the spline.
        /// </summary>
        public Vector3 GetPositionOnSegment(int segment, float time, Space space = Space.World)
        {
            if (segment == segments.Count - 1) segment--;
            Vector3 a = segments[segment].GetPosition(ControlHandle.ANCHOR, space);
            Vector3 ata = segments[segment].GetPosition(ControlHandle.TANGENT_A, space);
            Vector3 b = segments[segment + 1].GetPosition(ControlHandle.ANCHOR, space);
            Vector3 btb = segments[segment + 1].GetPosition(ControlHandle.TANGENT_B, space);

            return BezierUtility.Cubic(a, ata, btb, b, time);
        }

        public Vector3 GetPosition(float time, Space space = Space.World)
        {
            int segment = GetSegment(time);
            return GetPositionOnSegment(segment - 1, SplineUtility.GetSegmentTime(segment, segments.Count, time), space);
        }

        /// <summary>
        /// Gets the local position from the cached position map. 
        /// This is faster than GetPosition(), but only works when useCachedPositions is enabled.
        /// </summary>
        public Vector3 GetPositionFastLocal(float time)
        {
            int count = PositionMapLocal.Length;
            if (count == 0)
                return GetPosition(time, Space.Self);

            float resolution = GetSplineResolution();
            float rawIndex = time / resolution;
            rawIndex = Mathf.Clamp(rawIndex, 0f, count - 1);

            int i0 = Mathf.FloorToInt(rawIndex);
            int i1 = Mathf.Min(i0 + 1, count - 1);
            float frac = rawIndex - i0;

            return Vector3.Lerp(PositionMapLocal[i0], PositionMapLocal[i1], frac);
        }

        /// <summary>
        /// Like GetPosition(), but also supports values below 0 and 
        /// above 1 by extending the spline from the start or end anchor.
        /// </summary>
        public Vector3 GetPositionExtended(float time)
        {
            int segmentIndex = GetSegment(time) - 1;
            Vector3 position = GetPosition(time);
            if (time >= 1)
            {
                segmentIndex = segments.Count - 1;
                position = segments[segmentIndex].GetPosition(ControlHandle.ANCHOR);
            }

            Vector3 direction = segments[segmentIndex].GetDirection();

            if (time < 0)
            {
                direction = -direction;
                time = Mathf.Abs(time);
            }
            else if (time > 1)
                time -= 1;
            else
                return position;

            return position + direction * (time * length);
        }

        /// <summary>
        /// Gets the closest point on the spline to a 
        /// world position and outputs the corresponding spline time.
        /// </summary>
        public Vector3 GetNearestPoint(Vector3 worldPoint, out float timeValue, 
                                                          float startTime = 0, 
                                                          float endTime = 1, 
                                                          int precision = 8, 
                                                          float steps100Meter = 5, 
                                                          bool ignoreYAxel = false, 
                                                          bool useFixedTime = false)
        {
            steps100Meter = 100 / length / steps100Meter;
            if (steps100Meter > 0.2f) steps100Meter = 0.2f;
            if (steps100Meter < 0.0001f) steps100Meter = 0.0001f;

            Vector3 position = GetNearestPointRough(worldPoint, steps100Meter, out timeValue, startTime, endTime, ignoreYAxel, useFixedTime);

            for (int i = precision; i > 0; i--)
            {
                steps100Meter = steps100Meter / 1.66f;
                float timeForwards = timeValue + steps100Meter;
                float timeBackwards = timeValue - steps100Meter;

                //Only snap when not looping. Will prevent the position from getting stuck close to 0/1.
                if (!loop)
                {
                    if (timeForwards > 1) timeForwards = 1;
                    if (timeBackwards < 0) timeBackwards = 0;
                }

                Vector3 pForward = GetPosition(useFixedTime ? TimeToFixedTime(timeForwards) : timeForwards);
                float dForward = ignoreYAxel ? Vector2.Distance(new Vector2(pForward.x, pForward.z), new Vector2(worldPoint.x, worldPoint.z)) : Vector3.Distance(pForward, worldPoint);

                Vector3 pBackwards = GetPosition(useFixedTime ? TimeToFixedTime(timeBackwards) : timeBackwards);
                float dBackwards = ignoreYAxel ? Vector2.Distance(new Vector2(pBackwards.x, pBackwards.z), new Vector2(worldPoint.x, worldPoint.z)) : Vector3.Distance(pBackwards, worldPoint);

                if (dForward > dBackwards)
                {
                    position = pBackwards;
                    timeValue = timeBackwards;
                }
                else
                {
                    position = pForward;
                    timeValue = timeForwards;
                }
            }

            timeValue = Mathf.Clamp(timeValue, startTime, endTime);
            return position;

            Vector3 GetNearestPointRough(Vector3 point, float steps100Meter, out float timeValue, float startTime = 0,
                                                                                                  float endTime = 1,
                                                                                                  bool ignoreYAxel = false,
                                                                                                  bool useFixedTime = false)
            {
                timeValue = -1;
                float distance = 999999;
                Vector3 position = Vector3.zero;

                for (float t = startTime; t < endTime; t += steps100Meter)
                {
                    float dCheck;
                    float t2 = t;

                    //Calcuate closest point from fixed time instead.
                    //Remmeber that the regular time can still be needed for other calculations.
                    if (useFixedTime)
                        t2 = TimeToFixedTime(t);

                    Vector3 bezierPoint = GetPosition(t2, Space.World);

                    if (ignoreYAxel)
                        dCheck = Vector2.Distance(new Vector2(bezierPoint.x, bezierPoint.z), new Vector2(point.x, point.z));
                    else
                        dCheck = Vector3.Distance(bezierPoint, point);

                    if (dCheck < distance)
                    {
                        timeValue = t;
                        distance = dCheck;
                        position = bezierPoint;
                    }
                }

                return position;
            }
        }

        /// <summary>
        /// Finds the closest points between this spline and another spline, 
        /// and returns both points with their corresponding spline times.
        /// This can be used to detect whether two splines are overlapping.
        /// </summary>
        public SplineClosestResult GetNearestPoints(Spline spline, int precision = 7, 
                                                                   float stepsPer100Meter = 20)
        {
            NativeArray<NativeSegment> segments = SplineUtilityNative.CreateNativeArray(this.segments, Space.World, Allocator.TempJob);
            NativeArray<NativeSegment> segments2 = SplineUtilityNative.CreateNativeArray(spline.segments, Space.World, Allocator.TempJob);
            SplineClosestResult result = SplineUtility.GetNearestPoints(segments, length, segments2, spline.length, precision, stepsPer100Meter);

            segments.Dispose();
            segments2.Dispose();

            return result;
        }

        /// <summary>
        /// Gets the closest point on the spline to a world position.
        /// </summary>
        public Vector3 GetNearestPoint(Vector3 worldPoint)
        {
            return GetNearestPoint(worldPoint, out _);
        }

        /// <summary>
        /// Gets the closest point on the spline to a world position using fixed time, 
        /// and outputs the corresponding fixedTime.
        /// Fixed time maps to the exact distance along the spline.
        /// </summary>
        public Vector3 GetNearestPointFixedTime(Vector3 worldPoint, out float fixedTime, 
                                                                    int precision = 8, 
                                                                    float stepsPer100Meter = 5, 
                                                                    bool ignoreYAxel = false)
        {
            return GetNearestPoint(worldPoint, out fixedTime, 0, 1, precision, stepsPer100Meter, ignoreYAxel, true);
        }

        /// <summary>
        /// Gets the closest point on the spline to a world position using fixed time, and outputs the corresponding fixedTime.
        /// Fixed time maps to the exact distance along the spline.
        /// </summary>
        public Vector3 GetNearestPointFixedTime(Vector3 worldPoint, out float fixedTime)
        {
            return GetNearestPoint(worldPoint, out fixedTime, 0, 1, 8, 5, false, true); ;
        }

        /// <summary>
        /// Gets the nearest time on the spline from a world point.
        /// </summary>
        public float GetNearestTime(Vector3 worldPoint, float startTime, float endTime, 
                                                                         int precision = 8, 
                                                                         float steps100Meter = 5, 
                                                                         bool ignoreYAxel = false, 
                                                                         bool useFixedTime = false)
        {
            GetNearestPoint(worldPoint, out float timeValue, startTime, endTime, precision, steps100Meter, ignoreYAxel, useFixedTime);
            return timeValue;
        }

        /// <summary>
        /// Gets the nearest time on the spline from a world point.
        /// </summary>
        public float GetNearestTime(Vector3 worldPoint, int precision = 8, 
                                                        float steps100Meter = 5)
        {
            return GetNearestTime(worldPoint, 0, 1, precision, steps100Meter);
        }

        /// <summary>
        /// Gets the nearest fixed time on the spline from a world point.
        /// Fixed time maps to the exact distance along the spline.
        /// </summary>
        public float GetNearestFixedTime(Vector3 worldPoint, int precision = 8, 
                                                             float steps100Meter = 5)
        {
            return GetNearestTime(worldPoint, 0, 1, precision, steps100Meter, false, true);
        }

        public Vector3 GetDirection(float time, Space space = Space.World)
        {
            if (time <= 0.00001f) 
                return -(segments[0].GetPosition(ControlHandle.TANGENT_B) - segments[0].GetPosition(ControlHandle.ANCHOR)).normalized;
            else if (time >= 0.99999f) 
                return -(segments[segments.Count - 1].GetPosition(ControlHandle.ANCHOR) - segments[segments.Count - 1].GetPosition(ControlHandle.TANGENT_A)).normalized;

            int segment = GetSegment(time);
            if (segment < 1) segment = 1;

            float segementTime = SplineUtility.GetSegmentTime(segment, segments.Count, time);
            Vector3 forwardDirection = BezierUtility.GetDirection(segments[segment - 1].GetPosition(ControlHandle.ANCHOR, space),
                                                     segments[segment - 1].GetPosition(ControlHandle.TANGENT_A, space),
                                                     segments[segment].GetPosition(ControlHandle.TANGENT_B, space),
                                                     segments[segment].GetPosition(ControlHandle.ANCHOR, space), segementTime);

            return forwardDirection;
        }

        /// <summary>
        /// Converts normalized time to fixed time. Fixed time is the corrected parameter that
        /// corresponds to the actual distance along the spline.
        /// For example, to get the world position exactly halfway along the spline length:
        /// float fixedTime = TimeToFixedTime(0.5f);
        /// Vector3 worldPoint = spline.GetPosition(fixedTime);
        /// </summary>
        public float TimeToFixedTime(float time)
        {
#if UNITY_EDITOR
            if (DistanceMap.Length == 0)
                return 0;
#endif

            return SplineUtilityNative.TimeToFixedTime(DistanceMap, GetSplineResolution(), time, loop);
        }

        /// <summary>
        /// Converts fixed time back to regular spline time, which is not correlated with distance.
        /// </summary>
        public float FixedTimeToTime(float fixedTime)
        {
            return SplineUtilityNative.FixedTimeToTime(DistanceMap, GetSplineResolution(), fixedTime, loop);
        }

        /// <summary>
        /// Gets the center point of all control point anchors on the spline.
        /// </summary>
        public Vector3 GetCenter(Space space = Space.World)
        {
            Vector3 center = Vector3.zero;
            foreach (Segment s in segments)
            {
                if (loop && s == segments[segments.Count - 1])
                    continue;

                center += s.GetPosition(ControlHandle.ANCHOR, space);
            }

            return center / (segments.Count - (loop ? 1 : 0));
        }

        /// <summary>
        /// Fills the provided array (size 3) with the spline normals at 
        /// the given fixedTime: X, Y, and Z directions.
        /// </summary>
        public void GetNormalNonAlloc(Vector3[] normal, float fixedTime, 
                                                        Space space = Space.World,
                                                        RotationMode rotationMode = RotationMode.FOLLOW_SPLINE)
        {
            // Get normal
            if (segments.Count == 1)
            {
                normal[1] = transform.up;
                normal[2] = segments[0].GetDirection();
                normal[0] = Vector3.Cross(normal[2], -normal[1]).normalized;

                return;
            }
            else
            {
                int segmentIndex = SplineUtility.GetSegmentIndex(SegmentCount, fixedTime);
                float segmentTime = SplineUtility.GetSegmentTime(segmentIndex, SegmentCount, fixedTime);
                (Vector3, Vector3, Vector3) n = SplineUtilityNative.GetNormal(segmentIndex, segmentTime, false, NativeSegmentsLocal, normalsLocal, normalsMapLocal, rotationMode);
                normal[0] = n.Item1;
                normal[1] = n.Item2;
                normal[2] = n.Item3;
            }

            if (rotationMode != RotationMode.LOCK_UPWARDS || rotationMode != RotationMode.FOLLOW_PROJECTION)
            {
                // Add rotation
                float degrees = math.degrees(GetZRotationDegrees(fixedTime));
                Quaternion rotation = Quaternion.Inverse(Quaternion.AngleAxis(degrees, normal[2]));
                normal[0] = rotation * normal[0];
                normal[1] = rotation * normal[1];
            }

            // Transform to world
            if (space == Space.World)
            {
                normal[0] = transform.TransformDirection(normal[0]);
                normal[1] = transform.TransformDirection(normal[1]);
                normal[2] = transform.TransformDirection(normal[2]);
            }

            if (GeneralUtility.IsEqual(normal[2], normal[1]))
            {
                normal[1] = Vector3.up;
                normal[2] = Vector3.forward;
                normal[0] = Vector3.right;
            }
        }

        /// <summary>
        /// Gets the interpolated Z rotation (in degrees) along the spline at the specified time.
        /// </summary>
        public float GetZRotationDegrees(float time)
        {
            int segment = Mathf.Clamp(GetSegment(time), 1, segments.Count - 1);
            float segementTime = SplineUtility.GetSegmentTime(segment, segments.Count, time);
            float reversedSegmentTime = Mathf.Clamp01(1 - segementTime);

            float contrast = segments[segment - 1].Contrast - segments[segment].Contrast;
            contrast = segments[segment].Contrast + contrast - (contrast * segementTime);

            //Smooth the rotation closer to segment start/end.
            float numerator = Mathf.Pow(segementTime, contrast);
            float denominator = numerator + Mathf.Pow(reversedSegmentTime, contrast);

            segementTime = numerator / denominator;

            //Get rotation value
            float rotDif = segments[segment - 1].ZRotation - segments[segment].ZRotation;
            return math.radians(segments[segment].ZRotation + rotDif - (rotDif * segementTime));
        }

        public void SetLoop(bool enable, bool modifySegments = true)
        {
            if (loop == enable)
                return;

            loop = enable;

            if (!modifySegments)
                return;

            if (enable)
            {
                Segment first = segments[0];
                Segment last = CreateSegment(segments.Count,
                                            first.GetPosition(ControlHandle.ANCHOR),
                                            first.TangentADistance,
                                            first.TangentBDistance,
                                            first.Rotation);

                last.Contrast = first.Contrast;
                last.Scale = first.Scale;
                last.Noise = first.Noise;
                last.SaddleSkew = first.SaddleSkew;
                last.ZRotation = first.ZRotation;
            }
            else
            {
                RemoveSegmentAt(segments.Count - 1);
            }

            MarkCacheDirty();
        }

        public void Join(Spline spline, JoinType joinType)
        {
            float threshold = 0.5f;

            if (joinType == JoinType.END_TO_START)
            {
                float distance = Vector3.Distance(segments[segments.Count - 1].GetPosition(ControlHandle.ANCHOR), spline.segments[0].GetPosition(ControlHandle.ANCHOR));
                float distanceThreshold = segments[segments.Count - 1].Length * threshold;
                distanceThreshold = Mathf.Max(distanceThreshold, 1);
                for (int i = 0; i < spline.segments.Count; i++)
                {
                    if (GeneralUtility.IsZero(distance, distanceThreshold) && i == 0)
                        continue;

                    Segment s = spline.segments[i];
                    AddSegment(spline.segments[i]);
                }
            }
            else if (joinType == JoinType.END_TO_END)
            {
                float distance = Vector3.Distance(segments[segments.Count - 1].GetPosition(ControlHandle.ANCHOR), spline.segments[spline.segments.Count - 1].GetPosition(ControlHandle.ANCHOR));
                float distanceThreshold = segments[segments.Count - 1].Length * threshold;
                distanceThreshold = Mathf.Max(distanceThreshold, 1);
                for (int i = spline.segments.Count - 1; i >= 0; i--)
                {
                    if (GeneralUtility.IsZero(distance, distanceThreshold) && i == spline.segments.Count - 1)
                        continue;

                    Segment s = spline.segments[i];
                    AddSegment(spline.segments[i]);
                    Vector3 tangentA = s.GetPosition(ControlHandle.TANGENT_A);
                    Vector3 tangentB = s.GetPosition(ControlHandle.TANGENT_B);
                    s.SetPosition(ControlHandle.TANGENT_A, tangentB);
                    s.SetPosition(ControlHandle.TANGENT_B, tangentA);
                }
            }
            else if (joinType == JoinType.START_TO_START)
            {
                float distance = Vector3.Distance(segments[0].GetPosition(ControlHandle.ANCHOR), spline.segments[0].GetPosition(ControlHandle.ANCHOR));
                float distanceThreshold = segments[0].Length * threshold;
                distanceThreshold = Mathf.Max(distanceThreshold, 1);
                for (int i = 0; i < spline.segments.Count; i++)
                {
                    if (GeneralUtility.IsZero(distance, distanceThreshold) && i == 0)
                        continue;

                    Segment s = spline.segments[i];
                    Vector3 tangentA = s.GetPosition(ControlHandle.TANGENT_A);
                    Vector3 tangentB = s.GetPosition(ControlHandle.TANGENT_B);
                    s.SetPosition(ControlHandle.TANGENT_A, tangentB);
                    s.SetPosition(ControlHandle.TANGENT_B, tangentA);
                    InsertSegment(0, s);
                }
            }
            else if (joinType == JoinType.START_TO_END)
            {
                float distance = Vector3.Distance(segments[0].GetPosition(ControlHandle.ANCHOR), spline.segments[spline.segments.Count - 1].GetPosition(ControlHandle.ANCHOR));
                float distanceThreshold = segments[0].Length * threshold;
                distanceThreshold = Mathf.Max(distanceThreshold, 1);
                for (int i = spline.segments.Count - 1; i >= 0; i--)
                {
                    if (GeneralUtility.IsZero(distance, distanceThreshold) && i == spline.segments.Count - 1)
                        continue;

                    Segment s = spline.segments[i];
                    InsertSegment(0, s);
                }
            }

            MarkCacheDirty();
        }

        public void Split(Spline newSpline, int segmentIndex)
        {
            newSpline.segments.Clear();

            for (int i = segmentIndex; i < segments.Count; i++)
            {
                Segment s = segments[i];

                if (i == segmentIndex)
                    newSpline.CreateSegment(0, s.GetPosition(ControlHandle.ANCHOR), 
                                               s.TangentADistance, 
                                               s.TangentBDistance,
                                               s.Rotation);
                else
                {
                    newSpline.AddSegment(s);
                }
            }

            for (int i = segments.Count - 1; i > segmentIndex; i--)
                segments.RemoveAt(i);

            if (transform.parent != null)
                newSpline.transform.parent = transform.parent;

            MarkCacheDirty();
            newSpline.MarkCacheDirty();
        }

        /// <summary>
        /// Detects if a spline object has crossed a segment that contains links 
        /// between the previous and current spline positions.
        /// Call this every frame and check if the links list contains any segments. 
        /// If it does, you can cross over to another
        /// spline by using: splineObject.transform.parent = newSegment.SplineParent.transform.
        /// For an exact position on the new spline without any visual lag, 
        /// use CalculateLinkCrossingZPosition() to set the spline object's new position.
        /// </summary>
        public void FindLinkCrossingsNonAlloc(List<Segment> links, Vector3 splinePosition, 
                                                            Vector3 previousSplinePosition, 
                                                            LinkFlags linkFlags, 
                                                            out Segment currentSegment)
        {
            //Handle loop
            if (loop && splinePosition.z > length)
            {
                int loops = Mathf.FloorToInt(splinePosition.z / length);
                splinePosition.z -= length * loops;
                previousSplinePosition.z -= length * loops;
            }

            float minZ = Mathf.Min(previousSplinePosition.z, splinePosition.z);
            float maxZ = Mathf.Max(previousSplinePosition.z, splinePosition.z);
            currentSegment = null;

            bool skipLast = (linkFlags & LinkFlags.SKIP_LAST) != 0;
            bool skipFirst = (linkFlags & LinkFlags.SKIP_FIRST) != 0;
            bool skipSelf = (linkFlags & LinkFlags.SKIP_SELF) != 0;

            for (int i = 0; i < segments.Count; i++)
            {
                Segment s = segments[i];

                if (s.LinkCount == 0)
                    continue;

                if ((s.zPosition > minZ && s.zPosition < maxZ) || splinePosition.z == s.zPosition)
                {
                    for (int i2 = 0; i2 < s.LinkCount; i2++)
                    {
                        Segment s2 = s.GetLinkAtIndex(i2);

                        if (s2.ignoreLink)
                            continue;

                        if (linkFlags != LinkFlags.NONE)
                        {
                            Spline spline2 = s2.SplineParent;
                            Segment s2Link = s2;
                            Segment s2First = spline2.segments[0];
                            Segment s2Last = spline2.segments[spline2.segments.Count - 1];

                            if (skipLast && s2Last == s2Link) continue;
                            if (skipFirst && s2First == s2Link) continue;
                            if (skipSelf && s == s2Link) continue;
                        }

                        links.Add(s2);
                    }

                    currentSegment = s;
                    return;
                }
            }
        }

        /// <summary>
        /// Detects if a spline object has crossed a segment that contains 
        /// links between the previous and current spline positions.
        /// Call this every frame and check if the links list contains any segments. 
        /// If it does, you can cross over to another
        /// spline by using: splineObject.transform.parent = newSegment.SplineParent.transform.
        /// For an exact position on the new spline without any visual lag, 
        /// use CalculateLinkCrossingZPosition() to set the spline object's new position.
        /// </summary>
        public void FindLinkCrossingsNonAlloc(List<Segment> links, Vector3 splinePosition, 
                                                                   Vector3 previousSplinePosition)
        {
            FindLinkCrossingsNonAlloc(links, splinePosition, previousSplinePosition, LinkFlags.NONE, out _);
        }

        /// <summary>
        /// After detecting a link crossing with FindLinkCrossingsNonAlloc(),
        /// use this to calculate the spline object's new Z position on the spline it crossed to.
        /// This helps prevent visual lag when switching to a new spline.
        /// </summary>
        public float CalculateLinkCrossingZPosition(Vector3 splinePosition, Segment fromSegment, 
                                                                            Segment toSegment)
        {
            float validatedZPosition = SplineUtility.GetValidatedPosition(splinePosition, length, loop).z;
            float dif = validatedZPosition - fromSegment.zPosition;
            return toSegment.zPosition + dif;
        }

        /// <summary>
        /// Creates and initializes a SplineObject deformation on the 
        /// given GameObject and registers it to this spline.
        /// </summary>
        public SplineObject CreateDeformation(GameObject go, Vector3 localSplinePosition, 
                                                             Quaternion localSplineRotation, 
                                                             Transform parent = null,
                                                             bool worldPositionStays = true)
        {
#if UNITY_EDITOR
            editorDisableOnChildrenChanged = true;
#endif
            SplineObject.defaultType = SplineObjectType.DEFORMATION;

            if (parent == null) go.transform.SetParent(transform, worldPositionStays);
            else go.transform.SetParent(parent, worldPositionStays);

            SplineObject so = null;

            for(int i = 0; i < go.GetComponentCount(); i++)
            {
                Component c = go.GetComponentAtIndex(i);

                if(c is SplineObject)
                {
                    so = (SplineObject)c;
                    so.Initalize();
                    break;
                }
            }

            if(so == null) so = go.AddComponent<SplineObject>();
            so.localSplinePosition = localSplinePosition;
            so.localSplineRotation = localSplineRotation;
            so.componentMode = ComponentMode.ACTIVE;
            so.meshMode = MeshMode.CREATED_AT_RUNTIME;

#if UNITY_EDITOR
            if(componentMode != ComponentMode.ACTIVE)
            {
                Debug.LogError($"[Spline Architect] Can't create deformation becouse the component setting is not set to active on {name}!");
                so.DestroyAllInstanceMeshes();
                Destroy(so);
                return null;
            }
            else
            {
                for (int i = 0; i < so.MeshContainerCount; i++)
                {
                    MeshContainer mc = so.GetMeshContainerAtIndex(i);
                    Mesh instanceMesh = mc.GetInstanceMesh();

                    if (instanceMesh != null && !instanceMesh.isReadable)
                    {
                        Debug.LogError($"[Spline Architect] Can't deformation mesh becouse no read/write access on '{so.name}' ({name}).");
                        so.DestroyAllInstanceMeshes();
                        Destroy(so);
                        return null;
                    }
                }
            }
#endif
            if (so.MeshContainerCount == 0)
            {
                so.transform.localPosition = localSplinePosition;
                so.transform.localRotation = localSplineRotation;
            }

            so.MarkVersionDirty();
            so.RenderMeshInternal(false);

#if UNITY_EDITOR
            editorDisableOnChildrenChanged = false;
#endif

            return so;
        }

        /// <summary>
        /// Creates and initializes SplineObject deformations for all
        /// children of a given GameObject and registers them to this spline.
        /// </summary>
        public void CreateDeformationsForChildren(GameObject parentGameObject,
                                                  List<SplineObject> childrenResult)
        {
            transformContainer.Clear();
            parentGameObject.GetComponentsInChildren(transformContainer);

            foreach(Transform t in transformContainer)
            {
                if (t == parentGameObject.transform)
                    continue;

                SplineObject so = CreateDeformation(t.gameObject, t.localPosition, t.localRotation, parentGameObject.transform);
                childrenResult.Add(so);
            }
        }

        /// <summary>
        /// Creates and initializes a SplineObject follower on the 
        /// given GameObject and registers it to this spline.
        /// </summary>
        public SplineObject CreateFollower(GameObject go, Vector3 localSplinePosition, 
                                                          Quaternion localSplineRotation, 
                                                          Transform parent = null,
                                                          bool worldPositionStays = true)
        {
#if UNITY_EDITOR
            editorDisableOnChildrenChanged = true;
#endif
            SplineObject.defaultType = SplineObjectType.FOLLOWER;

            if (parent == null) go.transform.SetParent(transform, worldPositionStays);
            else go.transform.SetParent(parent, worldPositionStays);

            SplineObject so = null;

            for (int i = 0; i < go.GetComponentCount(); i++)
            {
                Component c = go.GetComponentAtIndex(i);

                if (c is SplineObject)
                {
                    so = (SplineObject)c;
                    so.Initalize();
                    break;
                }
            }

            if (so == null) so = go.AddComponent<SplineObject>();
            so.localSplinePosition = localSplinePosition;
            so.localSplineRotation = localSplineRotation;
            so.componentMode = ComponentMode.ACTIVE;

#if UNITY_EDITOR
            if(componentMode != ComponentMode.ACTIVE)
            {
                Debug.LogError($"[Spline Architect] Can't create follower becouse the component setting is not set to active on {name}!");
                so.DestroyAllInstanceMeshes();
                Destroy(so);
                return null;
            }          
#endif
            so.MarkVersionDirty();
            so.RenderMeshInternal(false);

#if UNITY_EDITOR
            editorDisableOnChildrenChanged = false;
#endif

            return so;
        }

        /// <summary>
        /// Creates and initializes a SplineInstance 
        /// and registers it to this spline.
        /// </summary>
        public SplineInstance CreateSplineInstance(Mesh mesh, List<Material> materials, 
                                                              float3 splinePosition, 
                                                              quaternion splineRotation, 
                                                              Vector3 scale)
        {
            SplineInstance si = new SplineInstance(mesh, materials);
            si.SetParent(this);
            si.SplinePosition = splinePosition;
            si.SplineRotation = splineRotation;
            si.Scale = scale;
            return si;
        }

        public SplineInstance CreateSplineInstance(Mesh mesh, Material material, 
                                                              float3 splinePosition, 
                                                              quaternion splineRotation, 
                                                              Vector3 scale)
        {
            SplineInstance si = new SplineInstance(mesh, material);
            si.SetParent(this);
            si.SplinePosition = splinePosition;
            si.SplineRotation = splineRotation;
            si.Scale = scale;
            return si;
        }

        /// <summary>
        /// Finds the closest spline objects to a given position along 
        /// the spline and stores the results in the provided list.
        /// It can also search on linked splines and filter results 
        /// by direction and other options using the given flags.
        /// This is typically used for things like collision checks 
        /// in traffic or movement systems.
        /// </summary>
        public void DistanceToClosestSplineObjectNonAlloc(List<SplineSearchResult> searchResults, 
                                                          Vector3 searchSplinePosition, 
                                                          int maxResults, 
                                                          SplineSearchResultFlags searchFlags, 
                                                          bool includeNestedSplineObjects = false, 
                                                          bool includeIgnoredLinks = true)
        {
            searchSplinePosition = SplineUtility.GetValidatedPosition(searchSplinePosition, length, loop);

            bool searchClosestLinkForward = (searchFlags & SplineSearchResultFlags.SEARCH_CLOSEST_LINK_FORWARD) != 0;
            bool searchClosestLinkBackward = (searchFlags & SplineSearchResultFlags.SEARCH_CLOSEST_LINK_BACKWARD) != 0;

            bool searchForward = (searchFlags & SplineSearchResultFlags.SEARCH_FORWARD) != 0;
            bool searchBackward = (searchFlags & SplineSearchResultFlags.SEARCH_BACKWARD) != 0;

            bool needSameX = (searchFlags & SplineSearchResultFlags.NEED_SAME_X_POSITION) != 0;
            bool needSameY = (searchFlags & SplineSearchResultFlags.NEED_SAME_Y_POSITION) != 0;

            Check(this, searchSplinePosition, 0, false);

            if (!searchClosestLinkForward && !searchClosestLinkBackward)
                return;

            Segment closestSegment = null;
            float segmentDistanceCheck = 99999;
            float segmentOffset = 99999;

            //Get closest segment
            foreach (Segment s in segments)
            {
                float d = SplineUtility.GetValidatedZDistance(searchSplinePosition, new Vector3(0, 0, s.zPosition), length, loop);

                float absD = Mathf.Abs(d);
                if (segmentDistanceCheck <= absD)
                    continue;

                if (s.LinkCount == 0)
                    continue;

                if (!searchBackward && d <= 0f)
                    continue;

                if (!searchForward && d >= 0f)
                    continue;

                closestSegment = s;
                segmentDistanceCheck = absD;
                segmentOffset = d;
            }

            if (closestSegment == null)
                return;

            //Check all links on closest segment if it has any.
            for (int i = 0; i < closestSegment.LinkCount; i++)
            {
                Segment link = closestSegment.GetLinkAtIndex(i);

                if (link == null)
                    continue;

                if (link.SplineParent == this)
                    continue;

                if (!includeIgnoredLinks && link.ignoreLink)
                    continue;

                Check(link.SplineParent, new Vector3(0, 0, link.zPosition), segmentOffset, true);
            }

            void Check(Spline splineToCheck, Vector3 splinePositionToCheck, float offset, bool isLink)
            {
                List<SplineObject> splineObjectToSearch = splineToCheck.rootSplineObjects;
                if (includeNestedSplineObjects) splineObjectToSearch = splineToCheck.allSplineObjects;

                for (int i = 0; i < splineObjectToSearch.Count; i++)
                {
                    SplineObject so = splineObjectToSearch[i];

                    float d = SplineUtility.GetValidatedZDistance(splinePositionToCheck, so.splinePosition, length, loop);

                    if (needSameX && !GeneralUtility.IsEqual(splinePositionToCheck.x, so.splinePosition.x))
                        continue;

                    if (needSameY && !GeneralUtility.IsEqual(splinePositionToCheck.y, so.splinePosition.y))
                        continue;

                    if (!isLink)
                    {
                        if (!searchBackward && d < 0f)
                            continue;

                        if (!searchForward && d > 0f)
                            continue;
                    }
                    else
                    {
                        if (!searchClosestLinkBackward && d < 0f)
                            continue;

                        if (!searchClosestLinkForward && d > 0f)
                            continue;
                    }

                    float d2 = Mathf.Abs(d) + offset;

                    if (searchResults.Count == 0)
                    {
                        searchResults.Add(new SplineSearchResult(offset, d * -1, d2, so, isLink, d > 0));
                        continue;
                    }

                    bool inserted = false;
                    for (int i2 = 0; i2 < searchResults.Count; i2++)
                    {
                        if (Mathf.Abs(searchResults[i2].distanceFromSplinePoint) > d2)
                        {
                            searchResults.Insert(i2, new SplineSearchResult(offset, d * -1, d2, so, isLink, d > 0));
                            inserted = true;
                            break;
                        }
                    }

                    if (!inserted) searchResults.Add(new SplineSearchResult(offset, d * -1, d2, so, isLink, d > 0));
                    if (searchResults.Count > maxResults) searchResults.RemoveAt(searchResults.Count - 1);
                }
            }
        }

        public Vector3 SplinePositionToWorldPosition(Vector3 splinePosition, 
                                                     Transform parentTransform, 
                                                     float4x4 matrix, 
                                                     bool alignToEnd = false,
                                                     RotationMode rotationMode = RotationMode.FOLLOW_SPLINE)
        {
            directPointWorker.Add(splinePosition, matrix, alignToEnd, rotationMode);
            directPointWorker.Complete(out PointWorkerData pwd);
            if (parentTransform == null) return pwd.point;
            return parentTransform.TransformPoint(pwd.point);
        }

        public Vector3 SplinePositionToWorldPosition(Vector3 splinePosition)
        {
            return SplinePositionToWorldPosition(splinePosition, transform, float4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one));
        }

        public Vector3 WorldPositionToSplinePosition(Vector3 worldPosition, int precision = 8, 
                                                                            float steps = 5,
                                                                            bool canExtend = true)
        {
            if (segments.Count == 1)
                return worldPosition;

            //Closest point needs to be calculated in fixedTime and time.
            //Get fixedTime for z position
            Vector3 fixedPoint = GetNearestPoint(worldPosition, out float fixedTime, 0, 1, precision, steps, false, true);
            Vector3 point = GetNearestPoint(worldPosition, out float time, 0, 1, precision, steps, false, false);

            Vector3 backDirection = -segments[0].GetDirection();
            Vector3 frontDirection = segments[segments.Count - 1].GetDirection();
            float extendedDistance = 0;

            if (canExtend && !loop)
            {
                Vector3 extendBackPoint = LineUtility.GetNearestPoint(segments[0].GetPosition(ControlHandle.ANCHOR),
                                                                  backDirection,
                                                                  worldPosition,
                                                                  out float extendBackLine);

                Vector3 extendFrontPoint = LineUtility.GetNearestPoint(segments[segments.Count - 1].GetPosition(ControlHandle.ANCHOR),
                                                                   frontDirection,
                                                                   worldPosition,
                                                                   out float extendFrontLine);

                float distanceToExtendBack = Vector3.Distance(worldPosition, extendBackPoint);
                float distanceToExtendFront = Vector3.Distance(worldPosition, extendFrontPoint);
                float distanceToPoint = Vector3.Distance(worldPosition, fixedPoint);

                bool extendingBack = extendBackLine >= 0 && distanceToExtendBack < distanceToPoint && distanceToExtendBack < distanceToExtendFront;
                bool extendingFront = extendFrontLine >= 0 && distanceToExtendFront < distanceToPoint && distanceToExtendFront < distanceToExtendBack;

                if (time == 0) extendingBack = true;
                if (time == 1) extendingFront = true;

                if (extendingBack)
                {
                    extendedDistance = -Vector3.Distance(extendBackPoint, segments[0].GetPosition(ControlHandle.ANCHOR));
                    fixedTime = 0;
                    time = 0;
                }
                else if (extendingFront)
                {
                    extendedDistance = Vector3.Distance(extendFrontPoint, segments[segments.Count - 1].GetPosition(ControlHandle.ANCHOR));
                    fixedTime = 1;
                    time = 1;
                }
            }

            //GetNormalsNonAlloc() allready calculates zRotation. Dont do it again.
            GetNormalNonAlloc(normalsContainer, time, Space.World);
            //Needs to be time here else the position will be offseted and the x axel will be wrong.
            Vector3 p1 = GetPosition(Mathf.Clamp(time, 0, 1), Space.World);

            Vector3 xDirection = normalsContainer[0];
            Vector3 yDirection = normalsContainer[1];

            Vector3 closestXPos = LineUtility.GetNearestPoint(p1, xDirection, worldPosition, out float timeX);
            Vector3 closestYPos = LineUtility.GetNearestPoint(p1, yDirection, worldPosition, out float timeY);

            Vector3 splinePosition;
            splinePosition.x = Vector3.Distance(p1, closestXPos) * Mathf.Sign(timeX);
            splinePosition.y = Vector3.Distance(p1, closestYPos) * Mathf.Sign(timeY);
            splinePosition.z = length * fixedTime + extendedDistance;

            //Apply scale. This is perfect as long the Spline is scaled the same on all axels. Will still work if not but can be scewed.
            splinePosition.x /= transform.localScale.x;
            splinePosition.y /= transform.localScale.y;

            return splinePosition;
        }

        public Quaternion SplineRotationToWorldRotation(Quaternion rotation, float time)
        {
            float fixedTime = TimeToFixedTime(time);
            GetNormalNonAlloc(normalsContainer, fixedTime);
            Quaternion splineForwardRot = Quaternion.LookRotation(normalsContainer[2], normalsContainer[1]);
            return splineForwardRot * rotation;
        }

        public Quaternion WorldRotationToSplineRotation(Quaternion rotation, float time)
        {
            //Rotation
            float fixedTime = TimeToFixedTime(time);
            GetNormalNonAlloc(normalsContainer, fixedTime);
            Quaternion splineLocalRotation = Quaternion.LookRotation(normalsContainer[2], normalsContainer[1]);
            return Quaternion.Inverse(splineLocalRotation) * rotation;
        }

        public void TransformToCenter(out Vector3 dif)
        {
            Vector3 oldPosition = transform.position;

            Vector3 wantedWorldDelta = GetCenter() - oldPosition;
            Vector3 localDelta = transform.InverseTransformVector(wantedWorldDelta);

#if UNITY_EDITOR
            float gridSize = EGlobalSettings.GetGridSize();
            localDelta.x = GeneralUtility.RoundToClosest(localDelta.x, gridSize);
            localDelta.y = 0f;
            localDelta.z = GeneralUtility.RoundToClosest(localDelta.z, gridSize);
#endif

            Vector3 worldDelta = transform.TransformVector(localDelta);
            Vector3 newPosition = oldPosition + worldDelta;

            transform.position = newPosition;
            dif = worldDelta;

            foreach (Segment s in segments)
            {
                s.Translate(ControlHandle.ANCHOR, localDelta, Space.Self);
                s.Translate(ControlHandle.TANGENT_A, localDelta, Space.Self);
                s.Translate(ControlHandle.TANGENT_B, localDelta, Space.Self);
            }

            if (!GeneralUtility.IsZero(worldDelta, 0.001f))
                MarkCacheDirty();

#if UNITY_EDITOR
        Spline[] splines = transform.GetComponentsInChildren<Spline>();

            foreach (Spline s in splines)
            {
                if (s == this)
                    continue;

                UnityEditor.Undo.RecordObject(s.transform, "");
                s.transform.position += dif;
            }

            EHandleEvents.InvokeAfterTransformToCenter(this, dif);
#endif
        }

        public void ProjectSegmentsToSurface()
        {
            foreach (Segment s in segments)
            {
                s.MoveToSurface();
            }
        }
    }
}
