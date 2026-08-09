// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: TriangleLinkingJob.cs
//
// Author: Mikael Danielsson
// Date Created: 25-04-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Collections;

namespace SplineArchitect.Jobs
{
    [BurstCompile]
    public struct TriangleLinkingJob : IJob
    {
        public NativeArray<int> triangles;
        public NativeHashMap<int, int> vertextMap;

        [ReadOnly] public NativeArray<Vector3> vertices;

        public void Execute()
        {
            for(int i = 0; i < vertices.Length; i++)
            {
                int vertexHash = vertices[i].GetHashCode();

                if (vertextMap.TryGetValue(vertexHash, out int index))
                {
                    for (int j = 0; j < triangles.Length; j++)
                    {
                        if (triangles[j] == i) triangles[j] = index;
                    }
                }
                else vertextMap.Add(vertexHash, i);
            }
        }
    }
}
