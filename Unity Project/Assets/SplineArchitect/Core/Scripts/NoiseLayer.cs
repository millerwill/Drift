// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: NoiseLayer.cs
//
// Author: Mikael Danielsson
// Date Created: 25-11-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;

using UnityEngine;

namespace SplineArchitect
{
    [Serializable]
    public partial struct NoiseLayer
    {

        public NoiseType type;
        public Vector3 scale;
        public float seed;
        public int octaves;
        public float frequency;
        public float amplitude;
        public NoiseGroup group;
        public bool enabled;

        public NoiseLayer(NoiseType type, Vector3 scale, float seed, int octaves = 4, float frequency = 2, float amplitude = 2)
        {
            this.type = type;
            this.scale = scale;
            this.seed = seed;
            this.octaves = octaves;
            this.frequency = frequency;
            this.amplitude = amplitude;
            group = NoiseGroup.A;
            enabled = true;
    }
}
}