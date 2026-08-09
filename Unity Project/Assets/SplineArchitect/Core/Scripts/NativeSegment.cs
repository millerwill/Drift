// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: NativeSegment.cs
//
// Author: Mikael Danielsson
// Date Created: 29-01-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;

using UnityEngine;
using Unity.Mathematics;

namespace SplineArchitect
{
    public struct NativeSegment
    {
        public float3 anchor;
        public float3 tangentA;
        public float3 tangentB;
        public quaternion rotation;
        public bool isStatic;

        public float length;
        public float zPosition;
        public float zRot;
        public float contrast;
        public float noise;
        public float2 saddleSkew;
        public float2 scale;

        public NativeSegment(float3 anchor, float3 tangentA, float3 tangentB, quaternion rotation, bool isStatic)
        {
            this.anchor = anchor;
            this.tangentA = tangentA;
            this.tangentB = tangentB;
            this.rotation = rotation;
            this.isStatic = isStatic;

            this.length = 0;
            this.zPosition = 0;
            zRot = Segment.defaultZRotation;
            contrast = Segment.defaultContrast;
            noise = 0;
            saddleSkew = new float2(Segment.defaultSaddleSkewX, Segment.defaultSaddleSkewY);
            scale = new float2(Segment.defaultScale, Segment.defaultScale);
        }

        public NativeSegment(float3 anchor,
                              float3 tangentA,
                              float3 tangentB,
                              quaternion rotation,
                              bool isStatic,
                              float length,
                              float zPosition,
                              float zRot,
                              float contrast,
                              float noise,
                              float2 sadleSkew,
                              float2 scale)
        {
            this.anchor = anchor;
            this.tangentA = tangentA;
            this.tangentB = tangentB;
            this.rotation = rotation;
            this.isStatic = isStatic;

            this.length = length;
            this.zPosition = zPosition;
            this.zRot = zRot;
            this.contrast = contrast;
            this.noise = noise;
            this.saddleSkew = sadleSkew;
            this.scale = scale;
        }
    }
}
