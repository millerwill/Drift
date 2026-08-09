using System.Collections.Generic;
using System;

using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using Random = UnityEngine.Random;

namespace SplineArchitect.Examples
{
    [AddComponentMenu("SplineArchitect/Examples/InstanceSystem/InstanceHandler")]
    public class InstanceHandler : MonoBehaviour
    {
        public static InstanceHandler instance;

        [Serializable]
        public class Instance
        {
            public Mesh mesh;
            public List<Material> materials;
        }

        public enum EventType
        {
            CROSSING,
            RACHED_END
        }

        public struct InstanceEvent
        {
            public EventType type;
            public NativeSplineInstance instance;
        }

        [BurstCompile]
        private struct SetPositionJob : IJobParallelFor
        {
            public NativeArray<NativeSplineInstance> nativeSplineInstances;
            public NativeList<InstanceEvent>.ParallelWriter events;
            [ReadOnly] public float deltaTime;
            [ReadOnly] public float speed;
            [ReadOnly] public float splineLength;

            public void Execute(int i)
            {
                NativeSplineInstance nsi = nativeSplineInstances[i];
                float3 prevPos = nsi.SplinePosition;

                nsi.SplinePosition = new float3(nsi.SplinePosition.x, nsi.SplinePosition.y, nsi.SplinePosition.z + deltaTime * speed);

                if (nsi.SplinePosition.z > splineLength)
                {
                    InstanceEvent nsie = new InstanceEvent();
                    nsie.type = EventType.RACHED_END;
                    nsie.instance = nsi;
                    events.AddNoResize(nsie);
                }
                else if (nsi.DidCrossSegment(prevPos, nsi.SplinePosition))
                {
                    InstanceEvent nsie = new InstanceEvent();
                    nsie.type = EventType.CROSSING;
                    nsie.instance = nsi;
                    events.AddNoResize(nsie);
                }

                nativeSplineInstances[i] = nsi;
            }
        }

        public float speed;
        public float spawnSpeed = 1.0f;
        public float ranOffset = 2;
        public float ranOffsetIncreaseSpeed = 0.1f;
        public List<Instance> instances;
        public bool switchToBlack;
        public float switchToBlackTime;
        public Material blackMaterial;
        private Spline spline;

        private float switchToBlackTimer;
        public float spawnAmount;
        private float spawnTimer;

        void Start()
        {
            instance = this;
            spline = GetComponent<Spline>();
            spline.instancingOptimizationFlags = SplineOptimizationFlags.DISABLE_NOISE | 
                                                 SplineOptimizationFlags.DISABLE_SADDLE_SKEW | 
                                                 SplineOptimizationFlags.DISABLE_Z_ROTATION | 
                                                 SplineOptimizationFlags.DISABLE_SPLINE_PROFILE;
            spline.InstancingReceiveShadows = false;
        }

        void Update()
        {
            spawnAmount += Time.deltaTime * spawnSpeed;
            ranOffset += Time.deltaTime * ranOffsetIncreaseSpeed;
            spawnTimer += Time.deltaTime;
            switchToBlackTimer += Time.deltaTime;

            if (spawnTimer > 0.1f)
            {
                spawnTimer = 0;
                CreateInstances(spawnAmount);
            }

            foreach (KeyValuePair<ulong, InstanceBatch> item in spline.GetInstanceBatchesUnsafe())
            {
                InstanceBatch batch = item.Value;
                batch.Bounds = spline.controlPointsBounds;

                // Set positions
                NativeList<InstanceEvent> events = new NativeList<InstanceEvent>(batch.Count, Allocator.TempJob);
                SetPositionJob setPositionJob = new SetPositionJob()
                {
                    nativeSplineInstances = batch.NativeSplineInstances.AsArray(),
                    events = events.AsParallelWriter(),
                    speed = speed,
                    deltaTime = Time.deltaTime,
                    splineLength = spline.Length,
                };
                JobHandle jobHandle = setPositionJob.Schedule(batch.Count, 32);
                jobHandle.Complete();

                // Handle events
                for (int i = 0; i < events.Length; i++)
                {
                    InstanceEvent nsia = events[i];
                    NativeSplineInstance nsi = nsia.instance;
                    SplineInstance si = nsi.GetSplineInstance(spline);

                    if (nsia.type == EventType.CROSSING)
                    {
                        Segment segment = spline.GetSegmentAtIndex(nsi.closestSegmentIndex);

                        if (segment.LinkCount == 0)
                            continue;

                        Segment newSegment = segment.GetLinkAtIndex(Random.Range(0, segment.LinkCount));

                        si.SetParent(newSegment.SplineParent);
                        si.SplinePosition = new float3(nsi.SplinePosition.x, nsi.SplinePosition.y, newSegment.ZPosition);
                    }
                    else if (nsia.type == EventType.RACHED_END)
                    {
                        si.SplinePosition = new float3(nsi.SplinePosition.x, nsi.SplinePosition.y, 0);

                        if (switchToBlack && switchToBlackTimer > switchToBlackTime && si.GetMaterialAtIndex(0) != blackMaterial)
                            si.SetMaterialAtIndex(0, blackMaterial);
                    }
                }

                // Dispose
                events.Dispose();
            }
        }

        private void CreateInstances(float amount)
        {
            int a = Mathf.FloorToInt(amount);
            float r = Random.Range(0f, 1f);

            if (r < (amount - (float)a))
                a++;


            for (int i = 0; i < a; i++)
            {
                Create(0.4f, 1.0f, null);
            }

            void Create(float minScale, float maxScale, Material material)
            {
                int randomIndex = Random.Range(0, instances.Count);
                Mesh mesh = instances[randomIndex].mesh;

                if (material == null)
                {
                    int randomMaterialIndex = Random.Range(0, instances[randomIndex].materials.Count);
                    material = instances[randomIndex].materials[randomMaterialIndex];
                }

                float3 startPos = new float3(Random.Range(-ranOffset, ranOffset), 
                                             Random.Range(-ranOffset, ranOffset), 
                                             Random.Range(-ranOffset, ranOffset));
                float scale = Random.Range(minScale, maxScale);

                spline.CreateSplineInstance(mesh, material, startPos, quaternion.identity, 
                                                                      new Vector3(scale, scale, scale));
            }
        }
    }
}