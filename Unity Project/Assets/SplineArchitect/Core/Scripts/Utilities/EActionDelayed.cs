// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EActionDelayed.cs
//
// Author: Mikael Danielsson
// Date Created: 15-02-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

#if UNITY_EDITOR
using System.Collections.Generic;
using System;
using UnityEditor;

namespace SplineArchitect.Utility
{
    internal class EActionDelayed
    {
        internal enum ActionFlag
        {
            DELAY = 1 << 0,
            FRAMES = 1 << 1,
            EARLY = 1 << 2,
            LATE = 1 << 3
        }

        private static double lastTimeSinceStartup;
        private static double editorDeltaTime;
        private static List<EActionDelayed> delayedActionsLate = new List<EActionDelayed>();
        private static List<EActionDelayed> delayedActionsEarly = new List<EActionDelayed>();
        private static HashSet<int> ids = new HashSet<int>();
        private static bool assembliesReloaded = false;

        internal Action action;
        internal double delay;
        internal int frames;
        internal int id;
        internal ActionFlag actionFlag { get; private set; }

        internal EActionDelayed(Action action, double delay, int frames, ActionFlag actionType, int id)
        {
            this.action = action;
            this.delay = delay;
            this.frames = frames;
            this.id = id;
            this.actionFlag = actionType;
        }

        internal static void UpdateGlobalEarly()
        {
            if (!assembliesReloaded)
            {
                assembliesReloaded = true;
                lastTimeSinceStartup = EditorApplication.timeSinceStartup;
            }

            editorDeltaTime = EditorApplication.timeSinceStartup - lastTimeSinceStartup;
            lastTimeSinceStartup = EditorApplication.timeSinceStartup;

            if (!assembliesReloaded)
            {
                assembliesReloaded = true;
                return;
            }

            for (int i = delayedActionsEarly.Count - 1; i >= 0; i--)
            {
                EActionDelayed da = delayedActionsEarly[i];


                bool usesDelay = (da.actionFlag & ActionFlag.DELAY) != 0;
                bool usesFrames = (da.actionFlag & ActionFlag.FRAMES) != 0;

                if (usesDelay && da.delay <= 0)
                {
                    da.action();
                    ids.Remove(da.id);
                    delayedActionsEarly.Remove(da);
                    continue;
                }

                if (usesFrames && da.frames <= 0)
                {
                    da.action();
                    ids.Remove(da.id);
                    delayedActionsEarly.Remove(da);
                    continue;
                }

                da.frames--;
                da.delay -= editorDeltaTime;
            }
        }

        internal static void UpdateGlobalLate()
        {
            for (int i = delayedActionsLate.Count - 1; i >= 0; i--)
            {
                EActionDelayed da = delayedActionsLate[i];

                bool usesDelay = (da.actionFlag & ActionFlag.DELAY) != 0;
                bool usesFrames = (da.actionFlag & ActionFlag.FRAMES) != 0;

                if (usesDelay && da.delay <= 0)
                {
                    da.action();
                    ids.Remove(da.id);
                    delayedActionsLate.Remove(da);
                    continue;
                }

                if (usesFrames && da.frames <= 0)
                {
                    da.action();
                    ids.Remove(da.id);
                    delayedActionsLate.Remove(da);
                    continue;
                }

                da.frames--;
                da.delay -= editorDeltaTime;
            }
        }

        internal static void Add(Action action, double timeDelay, int frames, ActionFlag actionFlag, int id = 0)
        {
            if (ids.Contains(id))
                return;

            if(id != 0) 
                ids.Add(id);

            bool early = (actionFlag & ActionFlag.EARLY) != 0;
            bool late = (actionFlag & ActionFlag.LATE) != 0;

            if (early)
                delayedActionsEarly.Add(new EActionDelayed(action, timeDelay, frames, actionFlag, id));

            if (late || !early)
                delayedActionsLate.Add(new EActionDelayed(action, timeDelay, frames, actionFlag, id));
        }
    }
}
#endif