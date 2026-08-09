// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EConversionUtility.cs
//
// Author: Mikael Danielsson
// Date Created: 09-02-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEngine;
using Unity.Collections;

namespace SplineArchitect.Utility
{
    public class EConversionUtility
    {
        public static string CapitalizeString(string value)
        {
            string newS = "";

            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == '_')
                    newS += ' ';
                else if (i == 0)
                    newS += char.ToUpper(value[i]);
                else if (i != 0)
                    newS += char.ToLower(value[i]);
            }

            return newS;
        }

        public static float[] Convert2DArrayToFlat(float[,] array2D, int ySize, int xSize)
        {
            float[] arrayFlat = new float[ySize * xSize];

            for (int y = 0; y < ySize; y++)
            {
                for (int x = 0; x < xSize; x++)
                {
                    arrayFlat[y * xSize + x] = array2D[y, x];
                }
            }

            return arrayFlat;
        }

        public static float[] ConvertAlphamapToFlat(float[,,] array3D, int part, int ySize, int xSize)
        {
            float[] arrayFlat = new float[ySize * xSize];

            for (int y = 0; y < ySize; y++)
            {
                for (int x = 0; x < xSize; x++)
                {
                    arrayFlat[y * xSize + x] = array3D[y, x, part];
                }
            }

            return arrayFlat;
        }

        public static float[,] ConvertFlatArrayTo2D(float[] flatArray, int ySize, int xSize)
        {
            float[,] array2D = new float[ySize, xSize];

            for (int y = 0; y < ySize; y++)
            {
                for (int x = 0; x < xSize; x++)
                {
                    array2D[y, x] = flatArray[y * xSize + x];
                }
            }

            return array2D;
        }

        public static Vector2Int FlatArrayIndexTo2DIndex(int index, int width)
        {
            Vector2Int index2D = new Vector2Int(-1, -1);
            index2D.x = index % width;
            index2D.y = index / width;
            return index2D;
        }
    }
}