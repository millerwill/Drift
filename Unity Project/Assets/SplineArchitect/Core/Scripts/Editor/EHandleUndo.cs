// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleUndo.cs
//
// Author: Mikael Danielsson
// Date Created: 27-07-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace SplineArchitect
{
    public class EHandleUndo
    {
        public enum RecordType : byte
        {
            RECORD_OBJECT,
            REGISTER_COMPLETE_OBJECT
        }

        //General
        private static long undoTriggeredTime;
        private static List<Spline> markedForDestroy = new List<Spline>();
        private static string lastUsedName = "";

        private static string GetRecordName(string name)
        {
            if (name == null)
                return lastUsedName;
            else
                lastUsedName = name;

            return name;
        }

        private static void Record(Object target, string name, RecordType recordType)
        {
            if (UndoTriggered())
                return;

            //Dont know why, but instances of prefabs needs to always be recorded with RegisterCompleteObjectUndo. Else some very weird things can happen.
            if (recordType == RecordType.RECORD_OBJECT)
            {
                GameObject go = null;

                if (target is Spline)
                {
                    Spline spline = target as Spline;
                    go = spline.gameObject;
                }
                else if (target is SplineObject)
                {
                    SplineObject so = target as SplineObject;
                    go = so.gameObject;
                }

                if (go != null && EHandlePrefab.IsPartOfAnyPrefab(go))
                {
                    Undo.RegisterCompleteObjectUndo(target, GetRecordName(name));
                    return;
                }

                Undo.RecordObject(target, GetRecordName(name));
            }
            else
                Undo.RegisterCompleteObjectUndo(target, GetRecordName(name));
        }

        public static bool UndoTriggered(int acceptTime = 250)
        {
            if (undoTriggeredTime + acceptTime >= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                return true;

            return false;
        }

        public static void UpdateUndoTriggerTime()
        {
            undoTriggeredTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public static void RecordNow(Object target, string name = null, RecordType recordType = RecordType.RECORD_OBJECT)
        {
            if (name != null)
                Undo.SetCurrentGroupName(name);
            Record(target, name, recordType);
        }

        public static void RegisterCreatedObject(Object target, string name = null)
        {
            Undo.RegisterCreatedObjectUndo(target, name);
        }

        public static void RegisterCompleteObject(Object target, string name = null)
        {
            Undo.RegisterCompleteObjectUndo(target, name);
        }

        public static void RegisterCompleteObject(Object[] target, string name = null)
        {
            Undo.RegisterCompleteObjectUndo(target, name);
        }

        public static T AddComponent<T>(GameObject target) where T : Component
        {
            return Undo.AddComponent<T>(target);
        }

        public static void SetTransformParent(Transform transform, Transform newParent, string name = null)
        {
            Undo.SetTransformParent(transform, newParent, name);
        }

        public static void DestroyObjectImmediate(Object o)
        {
            Undo.DestroyObjectImmediate(o);
        }

        public static void MarkSplineForDestroy(Spline spline)
        {
            markedForDestroy.Add(spline);
        }

        public static void DestroyMarkedSplines()
        {
            for (int i = markedForDestroy.Count - 1; i >= 0; i--)
            {
                HandleRegistry.RemoveSpline(markedForDestroy[i]);

                if(markedForDestroy[i] != null && markedForDestroy[i].gameObject != null)
                    Undo.DestroyObjectImmediate(markedForDestroy[i].gameObject);
            }

            markedForDestroy.Clear();
        }
    }
}
