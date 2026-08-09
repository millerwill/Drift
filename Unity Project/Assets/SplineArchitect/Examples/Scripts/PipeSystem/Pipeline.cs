using System;
using System.Collections.Generic;

using UnityEngine;
using Object = UnityEngine.Object;

using SplineArchitect.Utility;
using SplineArchitect.Workers;

namespace SplineArchitect.Examples
{
    public class Pipeline
    {
        public enum SegmentType
        {
            NONE,
            FRONT,
            BACK
        }

        public Spline spline;
        private List<SplineObject> followers = new List<SplineObject>();
        private SplineObject firstHolder;
        private Segment cachedSegment = null;

        public Pipeline(Vector3 point, int index)
        {
            GameObject splineGo = new GameObject();
            splineGo.name = $"Pipeline ({index})";
            splineGo.transform.parent = PipelineHandler.instance.transform;

            Vector3 anchor1 = point;
            Vector3 anchor2 = point;
            float tangentDistance = 5;
            Quaternion rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            spline = SplineUtility.Create(splineGo, anchor1, tangentDistance, tangentDistance, rotation, anchor2, tangentDistance, tangentDistance, rotation);

            //Create start holder
            SplineObject so = spline.CreateFollower(Object.Instantiate(PipelineHandler.instance.holderPrefab),
                                                    PipelineHandler.instance.holderOffset,
                                                    Quaternion.identity, null);
            firstHolder = so;

            //Create start cover
            so = spline.CreateFollower(Object.Instantiate(PipelineHandler.instance.coverPrefab),
                                            PipelineHandler.instance.coverOffset,
                                            Quaternion.Euler(0, 180, 0), null);
            followers.Add(so);

            //Create end cover
            so = spline.CreateFollower(Object.Instantiate(PipelineHandler.instance.coverPrefab),
                                            PipelineHandler.instance.coverOffset,
                                            Quaternion.Euler(0, 180, 0), null);
            so.AlignToEnd = true;
            followers.Add(so);

            foreach (PipeObject pipeObject in PipelineHandler.instance.pipeObjects)
            {
                Population population = new Population(pipeObject.prefab, pipeObject.deform, PipelineHandler.instance.deformationInterval > 0);
                population.FillMode = pipeObject.fillMode;
                population.StartPadding = pipeObject.offset.z;
                population.XOffset = pipeObject.offset.x;
                population.YOffset = pipeObject.offset.y;
                population.MaxInstances = 500;
                population.SkipTangents = false;

                spline.AddPopulation(population);
            }
            spline.afterJobs += AfterJobs;
        }

        private void AfterJobs()
        {
            Segment last = spline.GetSegmentAtIndex(spline.SegmentCount - 1);
            Segment secondLast = spline.GetSegmentAtIndex(spline.SegmentCount - 2);

            if (spline.SegmentCount == 2 && GeneralUtility.IsEqual(last.GetPosition(ControlHandle.ANCHOR),
                                                       secondLast.GetPosition(ControlHandle.ANCHOR)))
            {
                foreach (SplineObject so in followers)
                    so.RenderMesh(false);
            }
            else
            {
                foreach (SplineObject so in followers)
                    so.RenderMesh(true);
            }
        }

        public void UpdatePosition(Vector3 point)
        {
            Segment last = spline.GetSegmentAtIndex(spline.SegmentCount - 1);
            Segment secondLast = spline.GetSegmentAtIndex(spline.SegmentCount - 2);

            if (spline.SegmentCount == 2)
            {
                last.SetAnchorPosition(point);
                Vector3 direction = secondLast.GetPosition(ControlHandle.ANCHOR) - last.GetPosition(ControlHandle.ANCHOR);
                direction = direction.normalized;
                last.SetPosition(ControlHandle.TANGENT_A, point - direction);
                last.SetPosition(ControlHandle.TANGENT_B, point + direction);

                Vector3 secondLastPoint = secondLast.GetPosition(ControlHandle.ANCHOR);
                secondLast.SetPosition(ControlHandle.TANGENT_A, secondLastPoint - direction);
                secondLast.SetPosition(ControlHandle.TANGENT_B, secondLastPoint + direction);
            }
            else if (spline.SegmentCount > 2)
            {
                last.SetAnchorPosition(point);

                Vector3 lastAnchor = last.GetPosition(ControlHandle.ANCHOR);
                Vector3 secondLastAnchor = secondLast.GetPosition(ControlHandle.ANCHOR);

                float distance = Vector3.Distance(secondLastAnchor, lastAnchor);
                if (GeneralUtility.IsZero(distance)) distance = 1;

                Vector3 directionPos = spline.GetPositionOnSegment(spline.SegmentCount - 1, 0.75f);
                Vector3 direction = (lastAnchor - directionPos).normalized;
                Vector3 direction2 = secondLast.GetDirection();

                secondLast.SetPosition(ControlHandle.TANGENT_A, secondLastAnchor + direction2 * (distance * 0.75f));
                last.SetPosition(ControlHandle.TANGENT_A, lastAnchor + direction * (distance * 0.3f));
                last.SetPosition(ControlHandle.TANGENT_B, lastAnchor - direction * (distance * 0.3f));
            }
        }

        public void Enable()
        {
            if(cachedSegment != null)
            {
                spline.AddSegment(cachedSegment);
                cachedSegment = null;
                UpdatePosition(PipelineHandler.instance.GetIndicatorPosition());

                //Deform now to avoid visual glitches
                spline.DeformSplineObjectsNow();
            }

            spline.enabled = true;
        }

        public void Disable()
        {
            if(spline.SegmentCount > 2)
            {
                cachedSegment = spline.GetSegmentAtIndex(spline.SegmentCount - 1);
                spline.RemoveSegmentAt(spline.SegmentCount - 1);
            }

            //Deform now to avoid visual glitches
            spline.DeformSplineObjectsNow();
            spline.enabled = false;
        }

        public void PreviewLinking(Vector3 point, Pipeline pipeline, SegmentType segmentType)
        {
            Segment last = spline.GetSegmentAtIndex(spline.SegmentCount - 1);
            last.SetAnchorPosition(point);

            if (segmentType == SegmentType.FRONT)
            {
                int pipelineIndex = pipeline.spline.SegmentCount - 1;
                int index = spline.SegmentCount - 1;
                float tLength = Vector3.Distance(spline.GetSegmentAtIndex(index - 1).GetPosition(ControlHandle.ANCHOR),
                                                 pipeline.spline.GetSegmentAtIndex(pipelineIndex).GetPosition(ControlHandle.ANCHOR));
                tLength *= 0.5f;
                if (tLength < 1) tLength = 1;
                Vector3 dir = pipeline.spline.GetSegmentAtIndex(pipelineIndex).GetDirection();
                Vector3 a = pipeline.spline.GetSegmentAtIndex(pipelineIndex).GetPosition(ControlHandle.ANCHOR);

                spline.GetSegmentAtIndex(index).SetPosition(ControlHandle.TANGENT_B, a + dir * tLength);
                spline.GetSegmentAtIndex(index).SetPosition(ControlHandle.TANGENT_A, a - dir * tLength);
            }
            else if (segmentType == SegmentType.BACK)
            {
                int index = spline.SegmentCount - 1;

                float tLength = Vector3.Distance(spline.GetSegmentAtIndex(index - 1).GetPosition(ControlHandle.ANCHOR),
                                                 pipeline.spline.GetSegmentAtIndex(0).GetPosition(ControlHandle.ANCHOR));
                tLength *= 0.5f;
                if (tLength < 1) tLength = 1;
                Vector3 dir = pipeline.spline.GetSegmentAtIndex(0).GetDirection();
                Vector3 a = pipeline.spline.GetSegmentAtIndex(0).GetPosition(ControlHandle.ANCHOR);
                spline.GetSegmentAtIndex(index).SetPosition(ControlHandle.TANGENT_B, a - dir * tLength);
                spline.GetSegmentAtIndex(index).SetPosition(ControlHandle.TANGENT_A, a + dir * tLength);
            }
        }

        public void LinkAndDisable(Pipeline pipeline, SegmentType segmentType)
        {
            if (segmentType == SegmentType.FRONT)
            {
                int pipelineIndex = pipeline.spline.SegmentCount - 1;
                int index = spline.SegmentCount - 1;
                Vector3 anchor = spline.GetSegmentAtIndex(index).GetPosition(ControlHandle.ANCHOR);
                PreviewLinking(anchor, pipeline, segmentType);

                spline.GetSegmentAtIndex(index).LinkToSegment(anchor);
            }
            else if (segmentType == SegmentType.BACK)
            {
                int index = spline.SegmentCount - 1;
                Vector3 anchor = spline.GetSegmentAtIndex(index).GetPosition(ControlHandle.ANCHOR);
                PreviewLinking(anchor, pipeline, segmentType);

                spline.GetSegmentAtIndex(index).LinkToSegment(anchor);
            }

            //Deform now to avoid visual glitches
            spline.DeformSplineObjectsNow();
            spline.enabled = false;
        }

        public void CreateSegment(Vector3 point)
        {
            //Update last segments position before creating new segment
            UpdatePosition(point);

            //Calculate data for new pipeline
            Segment last = spline.GetSegmentAtIndex(spline.SegmentCount - 1);
            Vector3 anchor = last.GetPosition(ControlHandle.ANCHOR);
            Vector3 tangentA = last.GetPosition(ControlHandle.TANGENT_A);
            Vector3 tangentB = last.GetPosition(ControlHandle.TANGENT_B);
            //We can use static segments becouse the up rotation of the segment will always be world up.
            //Static segments do not need any cached normals and have better peformance becouse of this.
            spline.CreateStaticSegment(anchor, tangentA, tangentB);

            //We need to rebuild the cache to get the right spline length for the end holder
            spline.RebuildCache();

            //Create end holder
            SplineObject follower = spline.CreateFollower(Object.Instantiate(PipelineHandler.instance.holderPrefab),
                                new Vector3(0, 0, spline.Length) + PipelineHandler.instance.holderOffset,
                                Quaternion.identity,
                                null);

            if (spline.ContainsSplineObject(firstHolder))
                firstHolder.transform.parent = null;

            //Deform now to avoid visual glitches
            spline.DeformSplineObjectsNow();
            follower.enabled = false;
        }

        public SegmentType Hovering(Vector3 point)
        {
            SegmentType hoveringType = SegmentType.NONE;

            if (GeneralUtility.IsEqual(point, spline.GetSegmentAtIndex(0).GetPosition(ControlHandle.ANCHOR)) &&
                     spline.GetSegmentAtIndex(0).LinkTarget == LinkTarget.NONE)
                hoveringType = SegmentType.BACK;
            else if (GeneralUtility.IsEqual(point, spline.GetSegmentAtIndex(spline.SegmentCount - 1).GetPosition(ControlHandle.ANCHOR)) &&
                spline.GetSegmentAtIndex(spline.SegmentCount - 1).LinkTarget == LinkTarget.NONE)
                hoveringType = SegmentType.FRONT;

            return hoveringType;
        }
    }
}
