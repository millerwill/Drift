using System;
using System.Collections.Generic;

using UnityEngine;
using Unity.Collections;

using SplineArchitect.Utility;

namespace SplineArchitect.Examples
{
    [AddComponentMenu("SplineArchitect/Examples/PipeSystem/PipelineHandler")]
    public class PipelineHandler : MonoBehaviour
    {
        enum State     
        {
            IDLE,
            CREATING_PIPELINE,
        }

        [Header("Indicator")]
        public GameObject indicatorPrefabIdle;
        public GameObject indicatorPrefabCreating;
        public GameObject indicatorPrefabCantCreate;
        public GameObject indicatorPrefabClick;
        public Vector3 indicatorOffset;
        public LayerMask indicatorLayerMask;

        [Header("Settings")]
        public float gridSize;
        public float distanceThreshold;
        public int deformationInterval;
        public float splineResolution;

        [Header("Cover")]
        public GameObject coverPrefab;
        public Vector3 coverOffset;

        [Header("Section holder")]
        public GameObject holderPrefab;
        public Vector3 holderOffset;

        [Header("Pipe objects")]
        public List<PipeObject> pipeObjects;

        [Header("Debug")]
        public bool drawSlizeA;
        public bool drawSlizeB;
        public bool drawSlizeC;
        public bool drawCollisionPoints;

        //Indicators
        private GameObject indicatorIdle;
        private GameObject indicatorCreating;
        private GameObject indicatorCantCreate;
        private GameObject indicatorClick;

        //General
        private Pipeline activePipeline;
        private List<Pipeline> pipelines = new();
        private Plane projectionPlane = new Plane(Vector3.up, Vector3.zero);
        private State state = State.IDLE;

        //Debug gizmos
        private Vector3 gizmoSliceAStart;
        private Vector3 gizmoSliceAEnd;
        private Vector3 gizmoSliceBStart;
        private Vector3 gizmoSliceBEnd;
        private Vector3 gizmoSliceCStart;
        private Vector3 gizmoSliceCEnd;
        private Vector3 collResultPointA;
        private Vector3 collResultPointB;

        //Singelton
        public static PipelineHandler instance = null;

        //Public
        public Pipeline ActivePipeLine => activePipeline;

        private void OnEnable()
        {
            instance = this;
        }

        private void Start()
        {
            //Create indicators
            indicatorIdle = Instantiate(indicatorPrefabIdle);
            indicatorCreating = Instantiate(indicatorPrefabCreating);
            indicatorCantCreate = Instantiate(indicatorPrefabCantCreate);
            indicatorClick = Instantiate(indicatorPrefabClick);
        }

        private void Update()
        {
            Vector3 indicatorPos = GetIndicatorPosition();
            Pipeline.SegmentType segmentType = GetHoveredPipeline(indicatorPos, out Pipeline hoveredPipeline);

            float distance = GetDistanceToClosestPipeline(indicatorPos);

            if (state == State.IDLE)
            {
                if (HandleInput.IsMouseLeftDown() && distance >= distanceThreshold)
                {
                    //GO TO: CREATING_SPLINE
                    if (segmentType == Pipeline.SegmentType.FRONT)
                    {
                        hoveredPipeline.Enable();
                        activePipeline = hoveredPipeline;
                        state = State.CREATING_PIPELINE;
                    }
                    //GO TO: CREATING_SPLINE
                    else if (segmentType == Pipeline.SegmentType.NONE)
                    {
                        activePipeline = CreatePipeline(indicatorPos);
                        state = State.CREATING_PIPELINE;
                    }
                }
            }
            else if(state == State.CREATING_PIPELINE)
            {
                //GO TO: IDLE
                if (HandleInput.IsEscapeKeyDown())
                {
                    state = State.IDLE;
                    activePipeline.Disable();
                    activePipeline = null;
                    return;
                }

                activePipeline.spline.jobInterval = deformationInterval;
                activePipeline.spline.SetSplineResolution(splineResolution);

                if (segmentType != Pipeline.SegmentType.NONE) 
                    activePipeline.PreviewLinking(indicatorPos, hoveredPipeline, segmentType);
                else 
                    activePipeline.UpdatePosition(indicatorPos);

                if (HandleInput.IsMouseLeftDown() && (hoveredPipeline != activePipeline ||
                                                        hoveredPipeline == activePipeline && segmentType == Pipeline.SegmentType.BACK))
                {
                    //GO TO: IDLE
                    if (segmentType != Pipeline.SegmentType.NONE)
                    {
                        activePipeline.LinkAndDisable(hoveredPipeline, segmentType);
                        state = State.IDLE;
                        activePipeline = null;
                    }
                    else if(distance >= distanceThreshold)
                    {
                        activePipeline.CreateSegment(indicatorPos);
                    }
                }
            }

            //Update indicator
            indicatorClick.gameObject.SetActive(false);
            indicatorIdle.gameObject.SetActive(false);
            indicatorCreating.gameObject.SetActive(false);
            indicatorCantCreate.gameObject.SetActive(false);
            if (segmentType == Pipeline.SegmentType.BACK && state == State.IDLE ||
                segmentType == Pipeline.SegmentType.NONE && distance < distanceThreshold)
            {
                indicatorCantCreate.transform.position = indicatorPos + indicatorOffset;
                indicatorCantCreate.gameObject.SetActive(true);
            }
            else if (state == State.IDLE && segmentType != Pipeline.SegmentType.NONE ||
                     segmentType != Pipeline.SegmentType.NONE && activePipeline != hoveredPipeline || 
                     segmentType == Pipeline.SegmentType.BACK && activePipeline == hoveredPipeline)
            {
                indicatorClick.transform.position = indicatorPos + indicatorOffset;
                indicatorClick.gameObject.SetActive(true);
            }
            else
            {
                if (state == State.CREATING_PIPELINE)
                {
                    indicatorCreating.transform.position = indicatorPos + indicatorOffset;
                    indicatorCreating.gameObject.SetActive(true);
                }
                else
                {
                    indicatorIdle.transform.position = indicatorPos + indicatorOffset;
                    indicatorIdle.gameObject.SetActive(true);
                }
            }
        }

        private float GetDistanceToClosestPipeline(Vector3 indicatorPos)
        {
            gizmoSliceAStart = new Vector3(0, -25, 0);
            gizmoSliceAEnd = new Vector3(0, -25, 0);
            gizmoSliceBStart = new Vector3(0, -25, 0);
            gizmoSliceBEnd = new Vector3(0, -25, 0);
            gizmoSliceCStart = new Vector3(0, -25, 0);
            gizmoSliceCEnd = new Vector3(0, -25, 0);
            collResultPointA = new Vector3(0, -25, 0);
            collResultPointB = new Vector3(0, -25, 0);

            float distance = float.MaxValue;
            float disCheck = float.MaxValue;

            if (activePipeline == null)
                return distance;

            Spline spline = activePipeline.spline;

            if(spline.SegmentCount > 2)
                SelfCollisionTest();

            CollisionTest();

            return distance;

            void SelfCollisionTest()
            {
                //Create slices to avoid checking last segment against itself
                float startTime = spline.TimeToFixedTime(distanceThreshold * 1.5f / spline.Length);
                float endTime = spline.TimeToFixedTime(spline.GetSegmentAtIndex(spline.SegmentCount - 2).ZPosition / spline.Length);
                NativeList<NativeSegment> slicedSegmentsA = SplineUtility.CreateSlice(spline.NativeSegmentsLocal, startTime, endTime);

                //Create slize from last segment.
                startTime = spline.TimeToFixedTime((spline.GetSegmentAtIndex(spline.SegmentCount - 2).ZPosition / spline.Length) + (distanceThreshold * 1.5f / spline.Length));
                endTime = 1;
                NativeList<NativeSegment> slicedSegmentsB = SplineUtility.CreateSlice(spline.NativeSegmentsLocal, startTime, endTime);

                //Check distance between slices
                float lastSegmentLength = spline.GetSegmentAtIndex(spline.SegmentCount - 2).Length;
                SplineClosestResult result = SplineUtility.GetNearestPoints(slicedSegmentsA.AsArray(), spline.Length - lastSegmentLength,
                                                                            slicedSegmentsB.AsArray(), lastSegmentLength / 2,
                                                                            7, 25);
                distance = Vector3.Distance(result.pointA, result.pointB);
                disCheck = distance;
                collResultPointA = result.pointA;
                collResultPointB = result.pointB;
                gizmoSliceAStart = slicedSegmentsA[0].anchor;
                gizmoSliceAEnd = slicedSegmentsA[slicedSegmentsA.Length - 1].anchor;
                gizmoSliceBStart = slicedSegmentsB[0].anchor;
                gizmoSliceBEnd = slicedSegmentsB[slicedSegmentsB.Length - 1].anchor;

                slicedSegmentsA.Dispose();
                slicedSegmentsB.Dispose();
            }

            void CollisionTest()
            {
                float startTime2 = spline.TimeToFixedTime((distanceThreshold * 1.5f) / spline.Length);
                float endTime2 = 1;
                NativeList<NativeSegment> slicedSegmentsC = SplineUtility.CreateSlice(spline.NativeSegmentsLocal, startTime2, endTime2);

                foreach (Pipeline pipeline in pipelines)
                {
                    if (pipeline == activePipeline)
                        continue;

                    if (!spline.controlPointsBounds.Intersects(pipeline.spline.controlPointsBounds))
                        continue;

                    SplineClosestResult result2 = SplineUtility.GetNearestPoints(slicedSegmentsC.AsArray(), spline.Length - distanceThreshold,
                                                                                 pipeline.spline.NativeSegmentsLocal, pipeline.spline.Length,
                                                                                 7, 25);

                    float d = Vector3.Distance(result2.pointA, result2.pointB);

                    if (d < disCheck)
                    {
                        disCheck = d;
                        distance = d;
                        collResultPointA = result2.pointA;
                        collResultPointB = result2.pointB;
                    }
                }

                gizmoSliceCStart = slicedSegmentsC[0].anchor;
                gizmoSliceCEnd = slicedSegmentsC[slicedSegmentsC.Length - 1].anchor;

                slicedSegmentsC.Dispose();
            }
        }

        private Pipeline.SegmentType GetHoveredPipeline(Vector3 indicatorPos, out Pipeline pipeline)
        {
            Pipeline.SegmentType segmentType = Pipeline.SegmentType.NONE;
            pipeline = null;

            foreach (Pipeline p in pipelines)
            {
                if(activePipeline == p && activePipeline.spline.SegmentCount <= 2)
                    continue;

                Pipeline.SegmentType sType = p.Hovering(indicatorPos);

                if (activePipeline == p && sType != Pipeline.SegmentType.BACK)
                    continue;

                if (sType != Pipeline.SegmentType.NONE)
                {
                    pipeline = p;
                    segmentType = sType;
                    break;
                }
            }

            return segmentType;
        }

        private Pipeline CreatePipeline(Vector3 point)
        {
            Pipeline pipeline = new Pipeline(point, pipelines.Count + 1);
            pipelines.Add(pipeline);

            return pipeline;
        }

        public Vector3 GetIndicatorPosition()
        {
            Vector3 pos = Vector3.zero;
            Vector3 mousePos = HandleInput.GetMousePosition();

            if (float.IsNaN(mousePos.x) || float.IsNaN(mousePos.y) ||
                float.IsInfinity(mousePos.x) || float.IsInfinity(mousePos.y))
                return pos;

            Ray mouseRay = Camera.main.ScreenPointToRay(mousePos);
            if (Physics.Raycast(mouseRay, out RaycastHit hitInfo, float.MaxValue, indicatorLayerMask))
            {
                pos += hitInfo.point;
            }
            else
            {
                if (projectionPlane.Raycast(mouseRay, out float enter2))
                    pos += mouseRay.GetPoint(enter2);
            }
            pos = GeneralUtility.RoundToClosest(pos, gridSize);

            return pos;
        }

        private void OnDrawGizmos()
        {
            float size = 0.5f;

            if (drawSlizeA)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(gizmoSliceAStart + new Vector3(0, 2.5f, 0), size);
                Gizmos.DrawSphere(gizmoSliceAEnd + new Vector3(0, 2.5f, 0), size);
            }

            if(drawSlizeB)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(gizmoSliceBStart + new Vector3(0, 3.5f, 0), size);
                Gizmos.DrawSphere(gizmoSliceBEnd + new Vector3(0, 3.5f, 0), size  );
            }

            if (drawSlizeC)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(gizmoSliceCStart + new Vector3(0, 4.5f, 0), size);
                Gizmos.DrawSphere(gizmoSliceCEnd + new Vector3(0, 4.5f, 0), size);
            }

            if (drawCollisionPoints)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawSphere(collResultPointA + new Vector3(0, 5.5f, 0), size);
                Gizmos.DrawSphere(collResultPointB + new Vector3(0, 5.5f, 0), size);
            }
        }
    }
}
