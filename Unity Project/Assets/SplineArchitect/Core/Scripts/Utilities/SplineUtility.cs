// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SplineUtility.cs
//
// Author: Mikael Danielsson
// Date Created: 11-09-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

using SplineArchitect.Jobs;

namespace SplineArchitect.Utility
{
    public static class SplineUtility
    {
        /// <summary>
        /// Creates a spline from a list of points. 
        /// </summary>
        public static Spline Create(GameObject go, List<Vector3> segmentPoints, 
                                                   Color color = default, 
                                                   Space space = Space.Self)
        {
            if (segmentPoints.Count < 6)
            {
                throw new ArgumentException("The segment point list must contain at least 6 points.", nameof(segmentPoints));
            }

            if (segmentPoints.Count % 3 != 0)
            {
                throw new ArgumentException("The number of points must be a multiple of 3 because each segment uses 3 points: anchor, tangentA, and tangentB.", nameof(segmentPoints));
            }

            Spline spline = go.AddComponent<Spline>();

            for (int i = 0; i < segmentPoints.Count; i += 3)
            {
                Vector3 anchor = segmentPoints[i + 0];
                Vector3 tangentA = segmentPoints[i + 1];
                Vector3 tangentB = segmentPoints[i + 2];

                Vector3 forward = (anchor - tangentA).normalized;

                Quaternion rotation = Quaternion.LookRotation(forward, go.transform.up);

                float tADis = Vector3.Distance(anchor, tangentA);
                float tBDis = Vector3.Distance(anchor, tangentB);

                spline.CreateSegment(anchor, tADis, tBDis, rotation, space);
            }

            if(color != default) spline.color = color;
            spline.RebuildCache();

            return spline;
        }

        /// <summary>
        /// Creates a spline from a NativeList of NativeSegments. 
        /// </summary>
        public static Spline Create(GameObject go, NativeList<NativeSegment> nativeSegments, 
                                                   Color color = default, 
                                                   Space space = Space.Self)
        {
            if (nativeSegments.Length < 2)
            {
                throw new ArgumentException("Most contain at least 2 native segments.", nameof(nativeSegments));
            }

            Spline spline = go.AddComponent<Spline>();

            for (int i = 0; i < nativeSegments.Length; i++)
            {
                NativeSegment ns = nativeSegments[i];
                float tADis = Vector3.Distance(ns.anchor, ns.tangentA);
                float tBDis = Vector3.Distance(ns.anchor, ns.tangentB);

                Segment s = spline.CreateSegment(ns.anchor, tADis, tBDis, ns.rotation, space);
                s.ZRotation = ns.zRot;
                s.Contrast = ns.contrast;
                s.Noise = ns.noise;
                s.SaddleSkew = ns.saddleSkew;
                s.Scale = ns.scale;
            }

            if (color != default) spline.color = color;
            spline.RebuildCache();

            return spline;
        }

        /// <summary>
        /// Creates a spline from a List of NativeSegments. 
        /// </summary>
        public static Spline Create(GameObject go, List<NativeSegment> nativeSegments, 
                                                   Color color = default, 
                                                   Space space = Space.Self)
        {
            NativeList<NativeSegment> n = new NativeList<NativeSegment>(nativeSegments.Count, Allocator.TempJob);

            foreach(NativeSegment ns in nativeSegments)
                n.Add(ns);

            Spline spline = Create(go, n, color, space);
            n.Dispose();
            return spline;
        }

        /// <summary>
        /// Creates a spline from two segments defined by their control points.
        /// </summary>
        public static Spline Create(GameObject go, Vector3 anchor1, 
                                                   Vector3 tangentA1, 
                                                   Vector3 tangentB1,
                                                   Vector3 anchor2, 
                                                   Vector3 tangentA2, 
                                                   Vector3 tangentB2,
                                                   Color color = default)
        {
            Spline spline = go.AddComponent<Spline>();
            if (color != default) spline.color = color;

            float tA1Dis = Vector3.Distance(anchor1, tangentA1);
            float tB1Dis = Vector3.Distance(anchor1, tangentB1);
            Vector3 forward1 = (anchor1 - tangentA1).normalized;
            Quaternion rotation1 = Quaternion.LookRotation(forward1, go.transform.up);
            spline.CreateSegment(anchor1, tA1Dis, tB1Dis, rotation1);

            float tA2Dis = Vector3.Distance(anchor2, tangentA2);
            float tB2Dis = Vector3.Distance(anchor2, tangentB2);
            Vector3 forward2 = (anchor1 - tangentA1).normalized;
            Quaternion rotation2 = Quaternion.LookRotation(forward2, go.transform.up);
            spline.CreateSegment(anchor1, tA1Dis, tB1Dis, rotation2);

            spline.RebuildCache();

            return spline;
        }

        /// <summary>
        /// Creates a spline from two segments defined by their control points.
        /// </summary>
        public static Spline Create(GameObject go, Vector3 anchor1Pos,
                                                   float tangentA1Dis,
                                                   float tangentB1Dis,
                                                   Quaternion rotation1,
                                                   Vector3 anchor2Pos,
                                                   float tangentA2Dis,
                                                   float tangentB2Dis,
                                                   Quaternion rotation2,
                                                   Color color = default)
        {
            Spline spline = go.AddComponent<Spline>();
            if (color != default) spline.color = color;
            spline.CreateSegment(anchor1Pos, tangentA1Dis, tangentB1Dis, rotation1);
            spline.CreateSegment(anchor2Pos, tangentA2Dis, tangentB2Dis, rotation2);
            spline.RebuildCache();

            return spline;
        }

        /// <summary>
        /// Gets the nearest spline to a world point. 
        /// Get all registerer splines in the HandleRegsitry class.
        /// </summary>
        public static Spline GetNearestSpline(Vector3 point, HashSet<Spline> splines, 
                                                             float distanceToBounds, 
                                                             out float time, 
                                                             out Vector3 nearestPoint)
        {
            Spline closest = null;
            float closestD = 99999;
            time = 0;
            nearestPoint = Vector3.zero;

            foreach (Spline spline in splines)
            {
                if (!spline.IsEnabled())
                    continue;

                if(spline.segments == null || spline.segments.Count < 2)
                    continue;

                float boundsD = Vector3.Distance(spline.controlPointsBounds.ClosestPoint(point), point);

                if (boundsD > distanceToBounds)
                    continue;

                Vector3 nPoint = spline.GetNearestPoint(point, out float time2, 0, 1 , 8, 20);
                float d = Vector3.Distance(nPoint, point);

                if (d > distanceToBounds) 
                    continue;

                if (closestD > d)
                {
                    time = time2;
                    closestD = d;
                    closest = spline;
                    nearestPoint = nPoint;
                }
            }

            return closest;
        }

        public static Spline GetIntersectingSpline(Ray ray, HashSet<Spline> splines)
        {
            foreach (Spline spline in splines)
            {
                if (!spline.IsEnabled())
                    continue;

                if (spline.segments == null || spline.segments.Count < 2)
                    continue;

                if (!spline.controlPointsBounds.IntersectRay(ray))
                    continue;

                return spline;
            }

            return null;
        }

        /// <summary>
        /// Finds the closest points between two splines, 
        /// and returns both points with their corresponding spline times.
        /// This can be used to detect whether two splines are overlapping.
        /// </summary>
        public static SplineClosestResult GetNearestPoints(NativeArray<NativeSegment> segmentsSpline1, 
                                                           float spline1Length,
                                                           NativeArray<NativeSegment> segmentsSpline2, 
                                                           float spline2Length,
                                                           int precision = 7, 
                                                           float stepsPer100Meter = 20)
        {
            if(segmentsSpline1.Length < 2 || segmentsSpline2.Length < 2)
                throw new ArgumentException("Both splines must contain at least 2 segments.");

            NearestPointsSplinesJob nearestPointsSplinesJob = new NearestPointsSplinesJob()
            {
                points = new NativeArray<Vector3>(2, Allocator.TempJob),
                times = new NativeArray<double>(2, Allocator.TempJob),
                nativeSegmentsSpline1 = segmentsSpline1,
                spline1Length = spline1Length,
                nativeSegmentsSpline2 = segmentsSpline2,
                spline2Length = spline2Length,
                stepsPer100Meter = stepsPer100Meter,
                precision = precision
            };

            JobHandle jobHandle = nearestPointsSplinesJob.Schedule();
            jobHandle.Complete();

            SplineClosestResult result = new SplineClosestResult()
            {
                pointA = nearestPointsSplinesJob.points[0],
                pointB = nearestPointsSplinesJob.points[1],
                timeA = nearestPointsSplinesJob.times[0],
                timeB = nearestPointsSplinesJob.times[1]
            };

            nearestPointsSplinesJob.points.Dispose();
            nearestPointsSplinesJob.times.Dispose();

            return result;
        }

        /// <summary>
        /// Creates a slice of a spline defined by start and end time.
        /// </summary>
        public static NativeList<NativeSegment> CreateSlice(NativeArray<NativeSegment> nativeSegments, 
                                                            float startTime, 
                                                            float endTime, 
                                                            float epsilon = 0.0001f, 
                                                            Allocator allocator = Allocator.TempJob)
        {
            NativeList<NativeSegment> segments = new NativeList<NativeSegment>(allocator);

            startTime = Mathf.Clamp01(startTime);
            endTime   = Mathf.Clamp01(endTime);

            segments.AddRange(nativeSegments);

            bool addStartSegment = true;
            bool addEndSegment = true;
            int startIndex = 0;
            int endIndex = segments.Length - 1;

            for(int i = 0; i < nativeSegments.Length; i++)
            {
                float segmentTime = 0;

                if(i > 0) segmentTime = i / (float)(nativeSegments.Length - 1);

                if (addStartSegment && GeneralUtility.IsEqual(segmentTime, startTime, epsilon))
                {
                    addStartSegment = false;
                    startIndex = i;
                }

                if (addEndSegment && GeneralUtility.IsEqual(segmentTime, endTime, epsilon))
                {
                    addEndSegment = false;
                    endIndex = i;
                }

                if(!addStartSegment && !addEndSegment)
                    break;
            }

            if (addStartSegment)
            {
                AutoSmoothData asid = ComputeAutoSmoothData(nativeSegments, startTime);
                segments.InsertRange(asid.index, 1);
                segments[asid.index] = new NativeSegment() 
                { 
                    anchor = asid.anchor, tangentA = asid.tangentA, tangentB = asid.tangentB 
                };

                //Set indexes
                startIndex = asid.index;
                if(addEndSegment) endIndex = segments.Length - 1;
                else endIndex++;

                //Adjust next tangent b
                NativeSegment ns = segments[asid.index + 1];
                ns.tangentB = asid.nextTangentB;
                segments[asid.index + 1] = ns;
            }

            if (addEndSegment)
            {
                AutoSmoothData asid = ComputeAutoSmoothData(nativeSegments, endTime);
                if (addStartSegment) asid.index++;
                segments.InsertRange(asid.index, 1);
                segments[asid.index] = new NativeSegment() 
                { 
                    anchor = asid.anchor, tangentA = asid.tangentA, tangentB = asid.tangentB 
                };

                //Set index
                endIndex = asid.index;

                //Adjust prev tangent a
                if (addStartSegment && endIndex - 1 == startIndex)
                {
                    NativeSegment prev = segments[asid.index - 1];
                    NativeSegment curr = segments[asid.index];

                    float timeDif = endTime - startTime;
                    float oneSegmentTime = 1f / (nativeSegments.Length - 1);

                    float disToPrevA = Vector3.Distance(curr.anchor, prev.anchor);
                    float disToTB = Vector3.Distance(curr.anchor, curr.tangentB);

                    if(disToTB > disToPrevA)
                        curr.tangentB = curr.anchor - math.normalize(curr.anchor - curr.tangentB) * disToPrevA;

                    //Set prev tangent A
                    Vector3 halfWay = Vector3.Lerp(prev.anchor, curr.tangentB, 0.5f);
                    prev.tangentA = Vector3.Lerp(halfWay, prev.tangentA, timeDif / oneSegmentTime);
                    prev.tangentB = prev.anchor + math.normalize(prev.anchor - prev.tangentA);
                    segments[asid.index - 1] = prev;

                    //Set curr tangent B
                    halfWay = Vector3.Lerp(curr.anchor, curr.tangentB, 0.5f);
                    curr.tangentB = Vector3.Lerp(halfWay, curr.tangentB, timeDif / oneSegmentTime);
                    segments[asid.index] = curr;
                }
                else
                {
                    NativeSegment prev = segments[asid.index - 1];
                    prev.tangentA = asid.prevTangentA;
                    segments[asid.index - 1] = prev;
                }
            }

            for (int i = segments.Length - 1; i >= 0; i--)
            {
                if (startIndex <= i && endIndex >= i)
                    continue;

                segments.RemoveAt(i);
            }

            //Gurantee at least two segments
            if (segments.Length == 0)
            {
                Vector3 point = SplineUtilityNative.GetPosition(nativeSegments, startTime);
                NativeSegment n = new NativeSegment()
                {
                    anchor = point,
                    tangentA = point + Vector3.forward,
                    tangentB = point - Vector3.forward
                };
                segments.Add(n);
                segments.Add(n);
                return segments;
            }
            else if(segments.Length == 1)
            {
                segments.Add(segments[0]);
            }

            return segments;
        }

        /// <summary>
        /// Computes auto smooth data for inserting a new segment at specific time
        /// without modifiying the splines shapes.
        /// </summary>
        public static AutoSmoothData ComputeAutoSmoothData(NativeArray<NativeSegment> nativeSegements, 
                                                           float time)
        {
            time = Mathf.Clamp(time, 0, 1);

            int segmentCount = nativeSegements.Length;
            int index = GetSegmentIndex(segmentCount, time);

            float segmentTime = GetSegmentTime(index, segmentCount, time);

            NativeSegment prev = nativeSegements[index - 1];
            NativeSegment next = nativeSegements[index];

            Vector3 A = prev.anchor;
            Vector3 Ata = prev.tangentA;
            Vector3 Btb = next.tangentB;
            Vector3 B = next.anchor;

            Vector3 p1 = Vector3.Lerp(A, Ata, segmentTime);
            Vector3 p2 = Vector3.Lerp(Ata, Btb, segmentTime);
            Vector3 p3 = Vector3.Lerp(Btb, B, segmentTime);

            Vector3 newTangentA = Vector3.Lerp(p2, p3, segmentTime);
            Vector3 newTangentB = Vector3.Lerp(p1, p2, segmentTime);
            Vector3 newAnchor = Vector3.Lerp(newTangentB, newTangentA, segmentTime);

            AutoSmoothData autoSmooth = new AutoSmoothData()
            {
                index = index,
                anchor = newAnchor,
                tangentA = newTangentA,
                tangentB = newTangentB,
                prevTangentA = p1,
                nextTangentB = p3
            };

            return autoSmooth;
        }

        /// <summary>
        /// Collects all segments from a set of splines whose 
        /// anchor point is at the specified world position.
        /// Get all registerer splines in the HandleRegsitry class.
        /// </summary>
        public static void GetSegmentsAtPointNoAlloc(List<Segment> closestSegmentContainer, 
                                                     HashSet<Spline> activeSplines, 
                                                     Vector3 worldPoint, 
                                                     float epsilon = 0.001f)
        {
            closestSegmentContainer.Clear();

            foreach (Spline spline in activeSplines)
            {
                if (spline == null)
                    continue;

                if (!spline.gameObject.activeInHierarchy)
                    continue;

                Vector3 closestPoint = spline.controlPointsBounds.ClosestPoint(worldPoint);
                float distanceToBounds = Vector3.Distance(closestPoint, worldPoint);

                if (distanceToBounds > 15)
                    continue;

                for(int i = 0; i < spline.segments.Count; i++)
                {
                    Segment s = spline.segments[i];

                    if (spline.Loop && spline.segments.Count - 1 == i)
                        continue;

                    float d = Vector3.Distance(s.GetPosition(ControlHandle.ANCHOR), worldPoint);

                    if (GeneralUtility.IsZero(d, epsilon))
                    {
                        closestSegmentContainer.Add(s);
                    }
                }
            }
        }

        /// <summary>
        /// Calculates how detailed a spline segment should be 
        /// rendered based on its length and distance to the camera.
        /// </summary>
        public static int GetSegmentLinesCount(Vector3 anchorA, 
                                               Vector3 tangentA, 
                                               Vector3 tangentB, 
                                               Vector3 anchorB, 
                                               float segmentLength, 
                                               Camera camera, bool skipFrustumCheck = false)
        {
            //Settings
            const int testRange = 10;

            //General
            Vector3 cameraPoint = camera.transform.position;

            bool vpAInView = true;
            bool vpBInView = true;
            bool inView = false;

            Vector3 center = (anchorA + anchorB) / 2;
            float distanceToCamera = Vector3.Distance(center, cameraPoint);

            if (!skipFrustumCheck)
            {
                //First in view test
                Vector3 vpA = camera.WorldToViewportPoint(anchorA);
                Vector3 vpB = camera.WorldToViewportPoint(anchorB);
                vpAInView = vpA.x >= 0f && vpA.x <= 1f && vpA.y >= 0f && vpA.y <= 1f && vpA.z > 0f;
                vpBInView = vpB.x >= 0f && vpB.x <= 1f && vpB.y >= 0f && vpB.y <= 1f && vpB.z > 0f;

                //Skip
                if (segmentLength < testRange && distanceToCamera > testRange && !vpAInView && !vpBInView)
                    return 0;
            }

            //Second in view test
            int times = (int)(segmentLength / testRange);
            for (int i = 1; i < times; i++)
            {
                Vector3 point = BezierUtility.Cubic(anchorA, tangentA, tangentB, anchorB, (float)i / times);

                if(!skipFrustumCheck)
                {
                    Vector3 vp = camera.WorldToViewportPoint(point);
                    if (vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f && vp.z > 0f)
                        inView = true;
                }

                float dCheck = Vector3.Distance(point, cameraPoint);
                if (dCheck < distanceToCamera)
                    distanceToCamera = dCheck;
            }

#if UNITY_EDITOR
            //Skip if camera is too far away
            if (!skipFrustumCheck && distanceToCamera > EGlobalSettings.GetSplineViewDistance())
                return 0;
#endif

            //Skip
            if (distanceToCamera > testRange && !inView && !vpAInView && !vpBInView)
                return 0;

            Vector3 tADirection = (tangentA - anchorA).normalized;
            Vector3 tBDirection = (anchorB - tangentB).normalized;
            float distanceBetween = Vector3.Distance(anchorA, anchorB);

            //If segment is aligned draw one line
            if (GeneralUtility.IsEqual(tADirection, tBDirection) && GeneralUtility.IsEqual(anchorB, anchorA + tADirection * distanceBetween, 0.01f))
                return 1;

            //User modifier
            float userModifier = 1.25f;
#if UNITY_EDITOR
            userModifier = EGlobalSettings.GetSplineLineResolution() / 100;
#endif
            float clampedUserModification = Mathf.Clamp(userModifier, 0.1f, 10);

            //Distance modifier
            float boostedLength = GeneralUtility.BoostedValue(segmentLength, 100 * clampedUserModification, 0.7f);
            float t = distanceToCamera / boostedLength;

            if (camera.orthographic)
                t = camera.orthographicSize * 1.33f / boostedLength;

            float distanceModifier = Mathf.Lerp(13, 0, t);

            //Segment segmentLength modifier
            float time = Mathf.Clamp(segmentLength, 0, 100) / 100;
            float lengthModifier = Mathf.Lerp(0, 15, time) * Mathf.Clamp(100 / distanceToCamera, 0, 1);

            //Final
            float final = (distanceModifier + lengthModifier) * userModifier;
            final = Mathf.Clamp(final, 1 + (clampedUserModification * 2), 100);
            return (int)final;
        }

        /// <summary>
        /// Calculates how detailed a spline segment should be rendered based on its length and distance to the camera.
        /// </summary>
        public static int GetSegmentLinesCount(Segment segmentFrom, Segment segmentTo, Camera camera)
        {
            return GetSegmentLinesCount(segmentFrom.GetPosition(ControlHandle.ANCHOR), segmentFrom.GetPosition(ControlHandle.TANGENT_A),
                                     segmentTo.GetPosition(ControlHandle.TANGENT_B), segmentTo.GetPosition(ControlHandle.ANCHOR),
                                     segmentFrom.length, camera);
        }

        public static void AlignTangents(List<Segment> segments)
        {
            Vector3 tagentA = segments[0].GetPosition(ControlHandle.TANGENT_A);
            Vector3 tagentB = segments[0].GetPosition(ControlHandle.TANGENT_B);
            Vector3 direction = segments[0].GetDirection();

            for (int i = 1; i < segments.Count; i++)
            {
                float dot = Vector3.Dot(direction, segments[i].GetDirection());

                if (dot < 0)
                {
                    segments[i].SetPosition(ControlHandle.TANGENT_A, tagentB);
                    segments[i].SetPosition(ControlHandle.TANGENT_B, tagentA);
                }
                else
                {
                    segments[i].SetPosition(ControlHandle.TANGENT_A, tagentA);
                    segments[i].SetPosition(ControlHandle.TANGENT_B, tagentB);
                }
            }
        }

        public static NativeSegment CalculateAutoSmoothSegment(float tension, 
                                                               float roundness, 
                                                               NativeSegment nativeSegment, 
                                                               Vector3 prevAnchorPoint, 
                                                               bool usePrev, 
                                                               Vector3 nextAnchorPoint, 
                                                               bool useNext, bool isFirst = false)
        {
            if (!usePrev && !useNext)
                return nativeSegment;

            Vector3 current = nativeSegment.anchor;
            Vector3 previous;
            Vector3 next;

            if (!usePrev)
            {
                next = nextAnchorPoint;
                previous = current - (next - current);
            }
            else if (!useNext)
            {
                previous = prevAnchorPoint;
                next = current + (current - previous);
            }
            else
            {
                previous = prevAnchorPoint;
                next = nextAnchorPoint;
            }

            Vector3 tangentA = CalculateAutoSmoothTangent(previous, current, next, tension);
            Vector3 tangentB = CalculateAutoSmoothTangent(next, current, previous, tension);

            if (tangentA.sqrMagnitude > 0.000001f)
            {
                if (nativeSegment.isStatic)
                {
                    float disA = Vector3.Distance(nativeSegment.anchor, nativeSegment.tangentA);
                    float disB = Vector3.Distance(nativeSegment.anchor, nativeSegment.tangentB);
                    Vector3 dir = tangentA.normalized;
                    Vector3 a = nativeSegment.anchor;

                    nativeSegment.tangentA = a + dir * disA;
                    nativeSegment.tangentB = a - dir * disB;
                }

                Vector3 up = math.mul(nativeSegment.rotation, math.up());
                nativeSegment.rotation = quaternion.LookRotationSafe(tangentA.normalized, up);
            }

            Vector3 anchor = nativeSegment.anchor;
            Vector3 forward = math.mul(nativeSegment.rotation, math.forward());

            if (nextAnchorPoint != null)
                nativeSegment.tangentA = anchor + forward * tangentA.magnitude * roundness;

            if (prevAnchorPoint != null)
                nativeSegment.tangentB = anchor - forward * tangentB.magnitude * roundness;

            return nativeSegment;
        }

        public static Vector3 CalculateAutoSmoothTangent(Vector3 previous, 
                                                         Vector3 current, 
                                                         Vector3 next, 
                                                         float tension)
        {
            float d1 = Vector3.Distance(current, previous);
            float d2 = Vector3.Distance(next, current);

            if (d1 == 0f)
                return (next - current) * 0.1f;

            if (d2 == 0f)
                return (current - previous) * 0.1f;

            float a = tension;
            float twoA = 2f * tension;

            float d1PowA = Mathf.Pow(d1, a);
            float d1Pow2A = Mathf.Pow(d1, twoA);
            float d2PowA = Mathf.Pow(d2, a);
            float d2Pow2A = Mathf.Pow(d2, twoA);

            return (d1Pow2A * next - d2Pow2A * previous + (d2Pow2A - d1Pow2A) * current) / (3f * d1PowA * (d1PowA + d2PowA));
        }

        internal static int GetSegmentIndex(int length, float time)
        {
            int i = (int)math.ceil(time * (length - 1));
            i = math.clamp(i, 1, length - 1);
            return i;
        }

        internal static float GetSegmentTime(int segment, float segmentsCount, float time)
        {
            float value = 0;
            segmentsCount = segmentsCount - 1;

            if (time > 0)
            {
                float sectionTime = 1 / segmentsCount;
                float currentSectionTime = sectionTime - ((segment / segmentsCount) - time);
                value = currentSectionTime / sectionTime;
            }

            return value;
        }

        internal static Spline TryGetParentSpline(Spline spline)
        {
            Transform parent = spline.transform.parent;
            Spline parentSpline = null;

            for (int i = 0; i < 25; i++)
            {
                if(parent == null)
                    break;

                for(int i2 = 0; i2 < parent.gameObject.GetComponentCount(); i2++)
                {
                    Component c = parent.gameObject.GetComponentAtIndex(i2);
                    if(c as Spline)
                    {
                        parentSpline = c as Spline;
                        break;
                    }
                }

                if (parent == null)
                    break;

                parent = parent.parent;
            }

            return parentSpline;
        }

        internal static Spline TryFindSpline(Transform transform)
        {
            Spline spline = null;

            for (int i = 0; i < 25; i++)
            {
                if (transform == null)
                    break;

                spline = transform.GetComponent<Spline>();
                if (spline != null)
                    return spline;

                transform = transform.parent;
            }

            return spline;
        }

        internal static int SegmentIndexToControlPointId(int segementIndex, ControlHandle type)
        {
            return segementIndex * 3 + 1000 + ((int)type - 1);
        }

        internal static int ControlPointIdToSegmentIndex(int controlPointId)
        {
            int value = (controlPointId - 1000) % 3;
            return ((controlPointId - 1000) - value) / 3;
        }

        internal static ControlHandle GetControlHandleType(int controlPointId)
        {
            if (controlPointId == 0) return ControlHandle.NONE;
            else if (controlPointId % 3 == 0) return ControlHandle.TANGENT_B;
            else if (controlPointId % 3 == 2) return ControlHandle.TANGENT_A;
            else return ControlHandle.ANCHOR;
        }

        internal static ControlHandle GetOppositeTangentType(ControlHandle type)
        {
            return type == ControlHandle.TANGENT_A ? ControlHandle.TANGENT_B : ControlHandle.TANGENT_A;
        }

        internal static float GetValidatedTime(float time, bool loop)
        {
            if (!loop)
                return math.clamp(time, 0, 1);

            if (time < 0)
                time = 1 - math.abs(time);
            else if (time > 1)
                time = time - math.floor(time);

            return time;
        }

        internal static Vector3 GetValidatedPosition(Vector3 position, float splineLength, bool loop)
        {
            if (!loop) return position;

            if (position.z >= splineLength)
            {
                int loops = Mathf.FloorToInt(position.z / splineLength);
                position.z -= splineLength * loops;
            }

            if (position.z < 0)
            {
                int loops = Mathf.CeilToInt(Mathf.Abs(position.z) / splineLength);
                position.z += splineLength * loops;
            }

            return position;
        }

        internal static float GetValidatedZDistance(Vector3 point1, Vector3 point2, float splineLength, bool loop)
        {
            point1 = GetValidatedPosition(point1, splineLength, loop);
            point2 = GetValidatedPosition(point2, splineLength, loop);

            float a = point2.z - point1.z;

            if (!loop) return a;

            float b = point2.z + (splineLength - point1.z);
            float closestToZero = Mathf.Abs(a) < Mathf.Abs(b) ? a : b;
            float c = (point2.z - splineLength) - point1.z;
            closestToZero = Mathf.Abs(closestToZero) < Mathf.Abs(c) ? closestToZero : c;
            return closestToZero;
        }
    }
}
