using System;
using System.Collections.Generic;

using UnityEngine;

namespace SplineArchitect.Examples
{
    [Serializable]
    public class PipeObject
    {
        [Header("Prefab")]
        public GameObject prefab;

        [Header("Settings")]
        public Vector3 offset;
        public bool deform = true;
        public PopulationFillMode fillMode;

        [NonSerialized]
        private Bounds bounds;
        [NonSerialized]
        private bool boundsSet;

        public Bounds GetBounds()
        {
            if (boundsSet) return bounds;

            MeshFilter meshFilter = prefab.GetComponent<MeshFilter>();
            if (meshFilter)
            {
                bounds = meshFilter.sharedMesh.bounds;
                boundsSet = true;
            }

            MeshCollider meshCollider = prefab.GetComponent<MeshCollider>();

            if (meshCollider)
            {
                bounds = meshCollider.sharedMesh.bounds;
                boundsSet = true;
            }

            return bounds;
        }
    }
}
