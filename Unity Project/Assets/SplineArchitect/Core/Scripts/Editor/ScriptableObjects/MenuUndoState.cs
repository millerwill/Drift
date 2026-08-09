// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: MenuUndoState.cs
//
// Author: Mikael Danielsson
// Date Created: 10-07-2026
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace SplineArchitect.ScriptableObjects
{
    internal class MenuUndoState : ScriptableObject
    {
        // Spline
        [SerializeField] internal string activeSplineMenu = "deformation";
        [SerializeField] internal string activeAnchorMenu = "general";
        [SerializeField] internal bool schedulingMinimized = true;
        [SerializeField] internal bool renderingMinimized = true;
        [SerializeField] internal bool linksMinimized;
        [SerializeField] internal int selectedNoiseLayerIndex;

        // Spline Object
        [SerializeField] internal bool snapSettingsMinimized;
        [SerializeField] internal bool meshSettingsMinimized;
        [SerializeField] internal bool advancedSettingsMinimized;
        [SerializeField] internal string activeSplineObjectMenu = "general";
    }
}
