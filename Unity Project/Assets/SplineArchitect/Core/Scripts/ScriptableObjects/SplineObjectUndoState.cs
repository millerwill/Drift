// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SplineObjectUndoState.cs
//
// Author: Mikael Danielsson
// Date Created: 15-07-2026
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

#if UNITY_EDITOR

using System;
using System.Collections.Generic;

using UnityEngine;

namespace SplineArchitect.ScriptableObjects
{
    internal partial class SplineObjectUndoState : ScriptableObject
    {
        [SerializeField] internal int version;
        [NonSerialized] internal int oldVersion;
    }
}

#endif
