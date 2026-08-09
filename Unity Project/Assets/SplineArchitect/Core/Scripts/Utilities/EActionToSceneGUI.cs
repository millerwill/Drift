// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EActionToLateSceneGUI.cs
//
// Author: Mikael Danielsson
// Date Created: 01-07-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

#if UNITY_EDITOR
using System.Collections.Generic;
using System;
using UnityEngine;

namespace SplineArchitect.Utility
{
    internal static class EActionToSceneGUI
    {
        internal enum Type
        {
            LATE,
            EARLY,
            BEFORE
        }

        private static List<(Action, EventType, int)> actionsEarly = new List<(Action, EventType, int)>();
        private static List<(Action, EventType, int)> actionsLate = new List<(Action, EventType, int)>();
        private static List<(Action, EventType, int)> actionsBefore = new List<(Action, EventType, int)>();

        internal static void LateOnSceneGUI(Event e)
        {
            for (int i = actionsLate.Count - 1; i >= 0; i--)
            {
                if(actionsLate[i].Item2 == e.type)
                {
                    actionsLate[i].Item1();
                    actionsLate.Remove(actionsLate[i]);
                }
            }
        }

        internal static void EArlyOnSceneGUI(Event e)
        {
            for (int i = actionsEarly.Count - 1; i >= 0; i--)
            {
                if (actionsEarly[i].Item2 == e.type)
                {
                    actionsEarly[i].Item1();
                    actionsEarly.Remove(actionsEarly[i]);
                }
            }
        }

        internal static void BeforeOnSceneGUI(Event e)
        {
            for (int i = actionsBefore.Count - 1; i >= 0; i--)
            {
                if (actionsBefore[i].Item2 == e.type)
                {
                    actionsBefore[i].Item1();
                    actionsBefore.Remove(actionsBefore[i]);
                }
            }
        }

        internal static void Add(Action action, Type type, EventType eventType, int id = -1)
        {
            if(type == Type.EARLY)
            {
                if (id != -1 && actionsEarly.Exists(item => item.Item3 == id))
                    return;

                actionsEarly.Add((action, eventType, id));
            }
            else if (type == Type.LATE)
            {
                if (id != -1 && actionsLate.Exists(item => item.Item3 == id))
                    return;

                actionsLate.Add((action, eventType, id));
            }
            else 
            {
                if (id != -1 && actionsBefore.Exists(item => item.Item3 == id))
                    return;

                actionsBefore.Add((action, eventType, id));
            }
        }

        internal static void Insert(Action action, Type type, EventType eventType, int id = -1, int index = 0)
        {
            if (type == Type.EARLY)
            {
                if (id != -1 && actionsEarly.Exists(item => item.Item3 == id))
                    return;

                actionsEarly.Insert(index, (action, eventType, id));
            }
            else if (type == Type.LATE)
            {
                if (id != -1 && actionsLate.Exists(item => item.Item3 == id))
                    return;

                actionsLate.Insert(index, (action, eventType, id));
            }
            else
            {
                if (id != -1 && actionsBefore.Exists(item => item.Item3 == id))
                    return;

                actionsBefore.Insert(index, (action, eventType, id));
            }
        }
    }
}
#endif