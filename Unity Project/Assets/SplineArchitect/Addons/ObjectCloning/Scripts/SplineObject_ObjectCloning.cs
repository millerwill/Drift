// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SplineObject_Cloning.cs
//
// Author: Mikael Danielsson
// Date Created: 21-04-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;

namespace SplineArchitect
{
    public partial class SplineObject : MonoBehaviour
    {
#if UNITY_EDITOR

        //Stored data
        [HideInInspector, SerializeField] internal bool cloningEnabled;
        [HideInInspector, SerializeField] internal bool cloneUseFixedAmount;
        [HideInInspector, SerializeField] internal bool cloneSnapEnd;
        [HideInInspector, SerializeField] internal int cloneAmount;
        [HideInInspector, SerializeField] internal float cloneSnapEndOffset;
        [HideInInspector, SerializeField] internal Vector3 cloneOffset;
        [HideInInspector, SerializeField] internal CloneDirection cloneDirection;
        [HideInInspector, SerializeField] internal List<SplineObject> clones;
        [HideInInspector, SerializeField] internal List<SplineObject> originClones;

        internal void MarkCloneVersionDirty()
        {
            undoState.cloneVersion++;
        }

        internal bool IsCloneVersionDirty()
        {
            bool dirty = undoState.cloneVersion != undoState.oldCloneVersion;
            undoState.oldCloneVersion = undoState.cloneVersion;

            return dirty;
        }
#endif
    }
}
