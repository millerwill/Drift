// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: MonitorSpline.cs
//
// Author: Mikael Danielsson
// Date Created: 30-01-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using SplineArchitect.Utility;
using UnityEngine;

namespace SplineArchitect.Monitor
{
    internal class MonitorSpline
    {
        internal const int dataUsage = 32 + 8 + 4;

        private Spline spline;
        private float noiseSum;
        private Vector3 transformPos;
        private Quaternion transformRot;
        private Vector3 transformScale;

        internal MonitorSpline(Spline spline)
        {
            this.spline = spline;
            noiseSum = GetNoiseSum();
            transformPos = spline.transform.position;
            transformRot = spline.transform.rotation;
            transformScale = spline.transform.localScale;
#if UNITY_EDITOR
            EditorUpdateTransform();
            EditorUpdateChildCount();
#endif
        }

        private float GetNoiseSum()
        {
            float value = 0;

            foreach (Segment s in spline.segments)
            {
                value += s.Noise;
            }

            if (GeneralUtility.IsZero(value))
                return value;

            for (int i = 0; i < spline.noises.Count; i++)
            {
                if (!spline.noises[i].enabled)
                    continue;

                if (spline.noises[i].group != spline.noiseGroup)
                    continue;

                value += Mathf.Abs(spline.noises[i].scale.x);
                value += Mathf.Abs(spline.noises[i].scale.y);
                value += Mathf.Abs(spline.noises[i].scale.z);
                value += Mathf.Abs(spline.noises[i].octaves);
                value += Mathf.Abs(spline.noises[i].frequency);
                value += Mathf.Abs(spline.noises[i].amplitude);
                value += spline.noises[i].seed;
                value += (int)spline.noises[i].type;
            }

            return value;
        }

        public bool NoiseChange()
        {
            bool foundChange = false;

            float sum = GetNoiseSum();

            if (!GeneralUtility.IsEqual(sum, noiseSum, 0.001f)) 
                foundChange = true;

            noiseSum = sum;

            return foundChange;
        }

        public bool TransformChange()
        {
            bool foundChange = false;
            if (!GeneralUtility.IsEqual(transformPos, spline.transform.position)) foundChange = true;
            else if (!GeneralUtility.IsEqual(transformRot.eulerAngles, spline.transform.rotation.eulerAngles)) foundChange = true;
            else if (!GeneralUtility.IsEqual(transformScale, spline.transform.localScale)) foundChange = true;

            transformPos = spline.transform.position;
            transformRot = spline.transform.rotation;
            transformScale = spline.transform.localScale;

            return foundChange;
        }

#if UNITY_EDITOR
        private Vector3 editorTransformPos;
        private Quaternion editorTransformRot;
        private Vector3 editorTransformScale;
        private int childCount;

        public bool EditorTransformChange(bool forceUpdate = false)
        {
            bool foundChange = false;
            if (!GeneralUtility.IsEqual(editorTransformPos, spline.transform.position)) foundChange = true;
            else if (!GeneralUtility.IsEqual(editorTransformRot.eulerAngles, spline.transform.rotation.eulerAngles)) foundChange = true;
            else if (!GeneralUtility.IsEqual(editorTransformScale, spline.transform.localScale)) foundChange = true;

            if (forceUpdate)
            {
                editorTransformPos = spline.transform.position;
                editorTransformRot = spline.transform.rotation;
                editorTransformScale = spline.transform.localScale;
            }

            return foundChange;
        }

        public bool EditorChildCountChange(out int dif, bool forceUpdate = false)
        {
            dif = 0;

            bool foundChange = false;
            if (spline.transform.childCount != childCount)
            {
                dif = spline.transform.childCount - childCount;
                foundChange = true;
            }

            if (forceUpdate)
                childCount = spline.transform.childCount;

            return foundChange;
        }

        public void EditorUpdateTransform()
        {
            editorTransformScale = spline.transform.localScale;
            editorTransformPos = spline.transform.position;
            editorTransformRot = spline.transform.rotation;
        }

        public void EditorUpdateChildCount()
        {
            childCount = spline.transform.childCount;
        }
#endif
    }
}
