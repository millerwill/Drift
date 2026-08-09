// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: BuildProcessReport.cs
//
// Author: Mikael Danielsson
// Date Created: 18-01-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace SplineArchitect.PostProcessing
{
    public class BuildProcessReport : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        //0 = first
        public int callbackOrder { get { return 0; } }
        public static bool buildRunning = false;
        private static bool buildCompletedThisFrame = false;
        private static BuildReport report;

        public void OnPreprocessBuild(BuildReport _report)
        {
            report = _report;
            buildRunning = true;
            EHandleEvents.buildRunning = true;
        }

        public void OnPostprocessBuild(BuildReport _report)
        {
            buildCompletedThisFrame = true;
            buildRunning = false;
            EHandleEvents.buildRunning = false;
        }

        public static void UpdateGlobal()
        {
            if(report != null)
            {
                if (report.summary.result == BuildResult.Succeeded)
                {

                }
                else if (report.summary.result == BuildResult.Failed)
                {

                }
                else if (report.summary.result == BuildResult.Cancelled)
                {
                    UnityEditor.EditorUtility.RequestScriptReload();
                }
                else if (report.summary.result == BuildResult.Unknown)
                {

                }

                report = null;
            }

            buildCompletedThisFrame = false;
        }

        public static bool BuildCompletedThisFrame()
        {
            return buildCompletedThisFrame;
        }
    }
}
