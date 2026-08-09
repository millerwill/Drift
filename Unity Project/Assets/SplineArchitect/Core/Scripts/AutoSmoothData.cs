// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: AutoSmoothInsertData.cs
//
// Author: Mikael Danielsson
// Date Created: 29-12-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace SplineArchitect
{
    public struct AutoSmoothData
    {
        public int index;
        public Vector3 anchor;
        public Vector3 tangentA;
        public Vector3 tangentB;

        public Vector3 prevTangentA;
        public Vector3 nextTangentB;
    }
}
