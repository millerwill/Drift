// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: NoiseUtility.cs
//
// Author: Mikael Danielsson
// Date Created: 26-11-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

namespace SplineArchitect.Utility
{
    public static class NoiseUtility
    {
        public static float PerlinNoise(float xPos, 
                                        float zPos, 
                                        float scaleX, 
                                        float scaleZ)
        {
            scaleX *= 0.5f;
            scaleZ *= 0.5f;

            return Mathf.PerlinNoise(xPos * scaleX, zPos * scaleZ);
        }

        public static float BillowNoise(float xPos, 
                                        float zPos, 
                                        float scaleX, 
                                        float scaleZ)
        {
            scaleX *= 0.33f;
            scaleZ *= 0.33f;

            float result = Mathf.PerlinNoise(xPos * scaleX, zPos * scaleZ);
            result = 2f * Mathf.Abs(result - 0.5f);
            return result;
        }

        public static float TerraceNoise(float xPos, 
                                         float zPos, 
                                         float scaleX, 
                                         float scaleZ, 
                                         int steps)
        {
            scaleX *= 0.1f;
            scaleZ *= 0.1f;

            float result = Mathf.PerlinNoise(xPos * scaleX, zPos * scaleZ);
            result = Mathf.Floor(result * steps) / steps;
            return result;
        }

        public static float VoronoiNoise(float xPos, 
                                         float zPos, 
                                         float scaleX, 
                                         float scaleZ)
        {
            scaleX *= 4;
            scaleZ *= 4;

            float x = xPos * scaleX / 10;
            float z = zPos * scaleZ / 10;

            int xInt = Mathf.FloorToInt(x);
            int zInt = Mathf.FloorToInt(z);

            float minDist = float.MaxValue;

            for (int xi = xInt - 1; xi <= xInt + 1; xi++)
            {
                for (int zi = zInt - 1; zi <= zInt + 1; zi++)
                {
                    Vector2 cellPoint = new Vector2(
                        xi + Mathf.PerlinNoise(xi * 0.1f, zi * 0.1f),
                        zi + Mathf.PerlinNoise((xi + 100) * 0.1f, (zi + 100) * 0.1f)
                    );

                    Vector2 pos = new Vector2(x, z);
                    float dist = Vector2.SqrMagnitude(cellPoint - pos);

                    if (dist < minDist)
                    {
                        minDist = dist;
                    }
                }
            }

            return Mathf.Sqrt(minDist);
        }

        public static float DomainWarpedNoise(float xPos, 
                                              float zPos, 
                                              float scaleX, 
                                              float scaleZ, 
                                              float amplitude)
        {
            scaleX *= 0.35f;
            scaleZ *= 0.35f;

            //First layer of noise: Warping coordinates
            float warpX = Mathf.PerlinNoise(xPos * scaleX, zPos * scaleZ) * 2f - 1f; //Range [-1, 1]
            float warpZ = Mathf.PerlinNoise((xPos + 100f) * scaleX, (zPos + 100f) * scaleZ) * 2f - 1f; //Range [-1, 1]

            //Apply warp strength
            float warpedX = xPos + warpX * amplitude;
            float warpedZ = zPos + warpZ * amplitude;

            //Second layer of noise: Primary noise with warped coordinates
            float result = Mathf.PerlinNoise(warpedX * scaleX, warpedZ * scaleZ);

            //Scale the output and remap to the desired range
            return result;
        }

        public static float RidgedPerlinNoise(float xPos, 
                                              float zPos, 
                                              float scaleX, 
                                              float scaleZ, 
                                              int octaves, 
                                              float frequency, 
                                              float amplitude)
        {
            scaleX *= 0.1f;
            scaleZ *= 0.1f;

            float result = 0f;
            float offset = 1.0f;

            for (int i = 0; i < octaves; i++)
            {
                float value = Mathf.PerlinNoise(xPos * scaleX * frequency, zPos * scaleZ * frequency);
                value = offset - Mathf.Abs(value - offset);
                value *= value;
                result += value * amplitude;

                frequency *= 2f;
                amplitude *= 0.5f;
            }

            return result;
        }

        public static float FBMNoise(float xPos, float zPos, float scaleX, 
                                                             float scaleZ, 
                                                             int octaves, 
                                                             float frequency, 
                                                             float amplitude)
        {
            scaleX *= 0.2f;
            scaleZ *= 0.2f;
            amplitude *= 0.25f;
            frequency *= 0.5f;

            float result = 0f;
            for (int i = 0; i < octaves; i++)
            {
                float perlinValue = Mathf.PerlinNoise(xPos * scaleX * frequency, zPos * scaleZ * frequency);
                result += perlinValue * amplitude;
                amplitude *= 0.5f;
                frequency *= 2f;
            }

            return result;
        }

        public static float HybridMultifractal(float xPos, float zPos, 
                                                           float scaleX, 
                                                           float scaleZ, 
                                                           int octaves, 
                                                           float frequency, 
                                                           float amplitude)
        {
            scaleX *= 0.1f;
            scaleZ *= 0.1f;

            float offset = 0.7f;
            float gain = 20f;
            float weight = 1.0f;
            float result = 0.0f;

            for (int i = 0; i < octaves; i++)
            {
                float nx = xPos * scaleX * frequency;
                float nz = zPos * scaleZ * frequency;

                float signal = Mathf.PerlinNoise(nx, nz);
                signal = offset - Mathf.Abs(signal);
                signal *= signal;
                signal *= weight;
                weight = signal * gain;
                weight = Mathf.Clamp01(weight);

                result += signal * amplitude;

                frequency *= 2.0f;
                amplitude *= 0.5f;
            }

            return result;
        }

        internal static float GetNoiseValue(NativeArray<NoiseLayer> noises, 
                                                           Vector3 point, 
                                                           float modification, 
                                                           bool centerAroundZero = true, 
                                                           bool useYScale = true)
        {
            float value = 0;

            //NoiseLayer
            for (int i2 = 0; i2 < noises.Length; i2++)
            {
                NoiseLayer noise = noises[i2];
                float x = GetNoiseValue(noise, point);

                if (centerAroundZero)
                    x = math.lerp(-0.5f, 0.5f, x);

                if (useYScale)
                    x *= noise.scale.y;

                value += x * modification;
            }

            return value;
        }

        internal static float GetNoiseValue(NoiseLayer noise, Vector3 point)
        {
            float invertedScaleX = noise.scale.x != 0 ? 1 / noise.scale.x : 1;
            float invertedScaleZ = noise.scale.z != 0 ? 1 / noise.scale.z : 1;

            Vector3 newScale = new Vector3(invertedScaleX, noise.scale.y, invertedScaleZ);

            float value = 0;
            if (noise.type == NoiseType.PERLIN_NOISE)
                value = PerlinNoise(point.x + noise.seed, point.z + noise.seed, newScale.x, newScale.z);
            else if (noise.type == NoiseType.BILLOW_NOISE)
                value = BillowNoise(point.x + noise.seed, point.z + noise.seed, newScale.x, newScale.z);
            else if (noise.type == NoiseType.DOMAIN_WARPED_NOISE)
                value = DomainWarpedNoise(point.x + noise.seed, point.z + noise.seed, newScale.x, newScale.z, noise.amplitude);
            else if (noise.type == NoiseType.RIDGED_PERLIN_NOISE)
                value = RidgedPerlinNoise(point.x + noise.seed, point.z + noise.seed, newScale.x, newScale.z, noise.octaves, noise.frequency, noise.amplitude);
            else if (noise.type == NoiseType.FMB_NOISE)
                value = FBMNoise(point.x + noise.seed, point.z + noise.seed, newScale.x, newScale.z, noise.octaves, noise.frequency, noise.amplitude);
            else if (noise.type == NoiseType.HYBRID_MULTI_FRACTAL)
                value = HybridMultifractal(point.x + noise.seed, point.z + noise.seed, newScale.x, newScale.z, noise.octaves, noise.frequency, noise.amplitude);
            else if (noise.type == NoiseType.VORONOI_NOISE)
                value = VoronoiNoise(point.x + noise.seed, point.z + noise.seed, newScale.x, newScale.z);
            else if (noise.type == NoiseType.TERRACE_NOISE)
                value = TerraceNoise(point.x + noise.seed, point.z + noise.seed, newScale.x, newScale.z, 5);

            return value;
        }

        internal static float GetHeighestNoiseValue(NativeArray<NoiseLayer> noises, 
                                                                   Vector3 point)
        {
            float value = 0;

            for (int i2 = 0; i2 < noises.Length; i2++)
            {
                NoiseLayer noise = noises[i2];
                float x = GetNoiseValue(noise, point);

                if (x > value)
                    value = x;
            }

            return value;
        }
    }
}
