// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: Spline_Event.cs
//
// Author: Mikael Danielsson
// Date Created: 15-01-2026
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;

using UnityEngine;

namespace SplineArchitect
{
    public partial class Spline : MonoBehaviour
    {
        public event Action afterRebuildCache;
        public event Action beforeRebuildCache;
        public event Action afterJobs;
        public event Action beforeJobs;
        public event Action beforeSplineDestroy;

        internal void InvokeAfterRebuildCache()
        {
            afterRebuildCache?.Invoke();
        }

        internal void InvokeBeforeRebuildCache()
        {
            beforeRebuildCache?.Invoke();
        }

        internal void InvokeBeforeJobs()
        {
            beforeJobs?.Invoke();
        }

        internal void InvokeAfterJobs()
        {
            afterJobs?.Invoke();
        }

        internal void InvokeBeforeSplineDestroy()
        {
            beforeSplineDestroy?.Invoke();
        }
    }
}
