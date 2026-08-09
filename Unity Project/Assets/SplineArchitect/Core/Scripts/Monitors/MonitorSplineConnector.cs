// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: MonitorSplineConnector.cs
//
// Author: Mikael Danielsson
// Date Created: 23-05-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEngine;

using SplineArchitect.Utility;

namespace SplineArchitect.Monitor
{
    internal class MonitorSplineConnector
    {
        internal const int dataUsage = 24 + 16 + 12 + 64 + 64 + 8;

        private Quaternion rotation;
        private Vector3 position;
        private Dictionary<Segment, Vector3> segmentPosOffset = new Dictionary<Segment, Vector3>();
        private Dictionary<Segment, Vector3> segmentRotOffset = new Dictionary<Segment, Vector3>();
        private SplineConnector sc;

        internal MonitorSplineConnector(SplineConnector sc)
        {
            this.sc = sc;
            UpdateRotPos();

            void UpdateRotPos()
            {
                rotation = sc.transform.rotation;
                position = sc.transform.position;
            }
        }

        internal bool SegmentOffsetChange() 
        {
            bool foundChange = false;

            if(sc.ConnectionCount > segmentPosOffset.Count || sc.ConnectionCount > segmentRotOffset.Count)
            {
                for(int i = 0; i < sc.ConnectionCount; i++)
                {
                    Segment s = sc.GetConnectionAtIndex(i);

                    if(!segmentPosOffset.ContainsKey(s))
                        segmentPosOffset.Add(s, s.connectorPosOffset);

                    if(!segmentRotOffset.ContainsKey(s))
                        segmentRotOffset.Add(s, s.connectorEulerOffset);
                }
            }
            else if(sc.ConnectionCount < segmentPosOffset.Count || sc.ConnectionCount < segmentRotOffset.Count)
            {
                foundChange = true;
            }
            else
            {
                foreach (KeyValuePair<Segment, Vector3> item in segmentPosOffset)
                {
                    if (!GeneralUtility.IsEqual(item.Key.connectorPosOffset, item.Value))
                    {
                        foundChange = true;
                        break;
                    }
                }

                foreach (KeyValuePair<Segment, Vector3> item in segmentRotOffset)
                {
                    if (!GeneralUtility.IsEqual(item.Key.connectorEulerOffset, item.Value))
                    {
                        foundChange = true;
                        break;
                    }
                }
            }

            if (foundChange)
            {
                segmentPosOffset.Clear();
                segmentRotOffset.Clear();
                for (int i = 0; i < sc.ConnectionCount; i++)
                {
                    Segment s = sc.GetConnectionAtIndex(i);
                    segmentPosOffset.Add(s, s.connectorPosOffset);
                    segmentRotOffset.Add(s, s.connectorEulerOffset);
                }
            }

            return foundChange;
        }

        internal bool PosChange()
        {
            bool foundChange = false;
            if (!GeneralUtility.IsEqual(position, sc.transform.position)) foundChange = true;

            position = sc.transform.position;

            return foundChange;
        }

        internal bool RotChange()
        {
            bool foundChange = false;
            if (!GeneralUtility.IsEqual(rotation.eulerAngles, sc.transform.rotation.eulerAngles)) foundChange = true;

            rotation = sc.transform.rotation;

            return foundChange;
        }

        internal void UpdateOffsets()
        {
            segmentPosOffset.Clear();
            segmentRotOffset.Clear();
            for (int i = 0; i < sc.ConnectionCount; i++)
            {
                Segment s = sc.GetConnectionAtIndex(i);
                segmentPosOffset.Add(s, s.connectorPosOffset);
                segmentRotOffset.Add(s, s.connectorEulerOffset);
            }
        }
    }
}
