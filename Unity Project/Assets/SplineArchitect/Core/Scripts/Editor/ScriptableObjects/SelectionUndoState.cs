// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SelectionUndoState.cs
//
// Author: Mikael Danielsson
// Date Created: 10-07-2026
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEngine;

namespace SplineArchitect.ScriptableObjects
{
    internal class SelectionUndoState : ScriptableObject
    {
        [HideInInspector, SerializeField] internal int selectedControlPoint;
        [HideInInspector, SerializeField] internal List<int> selectedControlPoints = new List<int>();
    }
}
