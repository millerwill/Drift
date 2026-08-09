// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SnapData.cs
//
// Author: Mikael Danielsson
// Date Created: 22-05-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;

using UnityEngine;

namespace SplineArchitect
{
    public struct SnapData
    {
        public bool start;
        public bool end;
        public float snapStartPoint;
        public float snapEndPoint;
        public float soStartPoint;
        public float soEndPoint;
    }
}