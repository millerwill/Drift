// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SplineObjectUndoState_ObjectCloning.cs
//
// Author: Mikael Danielsson
// Date Created: 28-07-2026
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

#if UNITY_EDITOR

using System;

using UnityEngine;

namespace SplineArchitect.ScriptableObjects
{
    internal partial class SplineObjectUndoState : ScriptableObject
    {
        [SerializeField] internal int cloneVersion;
        [NonSerialized] internal int oldCloneVersion;
    }
}

#endif
