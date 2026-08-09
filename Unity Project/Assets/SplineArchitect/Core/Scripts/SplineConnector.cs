// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SplineConnector.cs
//
// Author: Mikael Danielsson
// Date Created: 22-05-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;

using SplineArchitect.Monitor;
using SplineArchitect.Utility;

namespace SplineArchitect
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("SplineArchitect/SplineConnector")]
    public partial class SplineConnector : MonoBehaviour
    {
        //Runtime data
        [NonSerialized] private List<Segment> connections;
        [NonSerialized] private MonitorSplineConnector monitor;
        [NonSerialized] private bool initalized;

        // Properties
        public int ConnectionCount => connections != null ? connections.Count : 0;
        internal MonitorSplineConnector Monitor => monitor;

        private void OnEnable()
        {
            Initalize();
        }

        private void OnDestroy()
        {
            HandleRegistry.RemoveSplineConnector(this);

            for (int i = ConnectionCount - 1; i >= 0; i--)
            {
                Segment s = connections[i];

#if UNITY_EDITOR
                if(s != null && s.SplineParent != null)
                    UnityEditor.Undo.RecordObject(s.SplineParent, "Remove Spline Connector");
#endif

                if (s != null)
                {
                    s.Unlink();
                }
            }

            if(connections != null)
                connections.Clear();
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (EHandleEvents.dragActive)
                return;

            if (EHandleEvents.isSplineConnectorSelected)
                return;
#endif
            bool posChange = Monitor.PosChange();
            bool rotChange = Monitor.RotChange();
            bool segmentOffsetChange = Monitor.SegmentOffsetChange();

            for (int i = ConnectionCount - 1; i >= 0; i--)
            {
                Segment s = connections[i];

                if (s == null || s.linkTarget != LinkTarget.SPLINE_CONNECTOR)
                {
                    connections.RemoveAt(i);
                    continue;
                }

                if (posChange || rotChange || segmentOffsetChange)
                {
                    AlignSegment(s);
                }
            }
        }

        internal void Initalize()
        {
#if UNITY_EDITOR
            if (EHandleEvents.dragActive)
            {
                EHandleEvents.InitalizeAfterDrag(this);
                return;
            }
#endif

            if (initalized)
                return;

            initalized = true;

            if (connections == null)
                connections = new List<Segment>();

            if (Monitor == null)
                monitor = new MonitorSplineConnector(this);

            HandleRegistry.AddSplineConnector(this);
        }

        internal void RemoveConnection(Segment segment)
        {
            connections.Remove(segment);
            Monitor.UpdateOffsets();
        }

        internal void AddConnection(Segment segment)
        {
            if (connections.Contains(segment))
                return;

            connections.Add(segment);
            Monitor.UpdateOffsets();
        }

        internal void AlignSegment(Segment segment)
        {
            Vector3 connectionPoint = transform.position + segment.connectorPosOffset;

            Vector3 a = segment.GetPosition(ControlHandle.ANCHOR);
            segment.SetPosition(ControlHandle.ANCHOR, connectionPoint);

            Vector3 ta = segment.GetPosition(ControlHandle.TANGENT_A);
            Vector3 tb = segment.GetPosition(ControlHandle.TANGENT_B);

            float taDistance = Vector3.Distance(a, ta);
            float tbDistance = Vector3.Distance(a, tb);

            Quaternion connectorRotation = transform.rotation * Quaternion.Euler(segment.connectorEulerOffset);
            Vector3 dir = connectorRotation * Vector3.forward;

            if (segment.IsStatic)
            {
                Vector3 baseUp = Vector3.ProjectOnPlane(segment.SplineParent.transform.up, dir).normalized;
                Vector3 connectorUp = Vector3.ProjectOnPlane(connectorRotation * Vector3.up, dir).normalized;
                segment.ZRotation = -Vector3.SignedAngle(baseUp, connectorUp, dir);
            }
            else
            {
                segment.Rotation = connectorRotation;
                segment.ZRotation = 0;
            }

            segment.SetPosition(ControlHandle.TANGENT_A, connectionPoint + dir * taDistance);
            segment.SetPosition(ControlHandle.TANGENT_B, connectionPoint - dir * tbDistance);

#if UNITY_EDITOR
            //Enables animations to be uodated even when a spline connector is not selected.
            if (!Application.isPlaying)
            {
                segment.SplineParent.MarkEditorCacheDirty();
            }
#endif
        }

        internal float GetZRotation()
        {
            float zRotation = transform.rotation.eulerAngles.z;
            if (zRotation > 180) zRotation -= 360;

            return zRotation;
        }

        public Segment GetConnectionAtIndex(int index)
        {
            return connections[index];
        }

        public bool ContainsConnection(Spline spline)
        {
            for(int i = 0; i < ConnectionCount; i++)
            {
                Segment s = connections[i];

                if (s.SplineParent == spline)
                    return true;
            }

            return false;
        }

        public bool ContainsConnection(Segment segment)
        {
            return connections.Contains(segment);
        }
    }
}