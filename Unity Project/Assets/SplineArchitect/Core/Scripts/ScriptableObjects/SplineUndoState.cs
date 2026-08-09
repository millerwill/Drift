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
    internal partial class SplineUndoState : ScriptableObject
    {
        [SerializeField] internal int cacheVersion;
        [NonSerialized] internal int oldCacheVersion;
        [SerializeField] internal int editorCacheVersion;
        [NonSerialized] internal int oldEditorCacheVersion;
    }
}

#endif
