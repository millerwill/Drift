// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: LibraryColor.cs
//
// Author: Mikael Danielsson
// Date Created: 09-12-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.IO;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using SplineArchitect.Utility;

namespace SplineArchitect.Libraries
{
    public class LibraryTexture
    {
        private static List<Texture2D> preloadedTextures = new List<Texture2D>();

        //Others
        public static Texture2D inputField { get; private set; }
        public static Texture2D inputFieldActive { get; private set; }
        public static Texture2D inputFieldDelayed { get; private set; }
        public static Texture2D inputFieldDelayedActive { get; private set; }
        public static Texture2D inputFieldPending { get; private set; }
        public static Texture2D popUpField { get; private set; }
        public static Texture2D infoBox { get; private set; }

        //Buttons
        public static Texture2D buttonDefault { get; private set; }
        public static Texture2D buttonDefaultActive { get; private set; }
        public static Texture2D buttonDefaultPressed { get; private set; }
        public static Texture2D buttonSubMenu { get; private set; }
        public static Texture2D buttonSubMenuActive { get; private set; }
        public static Texture2D buttonSubMenuPressed { get; private set; }
        public static Texture2D buttonDefaultRed { get; private set; }
        public static Texture2D buttonDefaultRedPressed { get; private set; }
        public static Texture2D buttonDefaultWhite { get; private set; }
        public static Texture2D buttonDefaultWhitePressed { get; private set; }
        public static Texture2D buttonDefaultGreen { get; private set; }
        public static Texture2D buttonDefaultGreenPressed { get; private set; }

        //Icons
        public static Texture2D iconAlignToEnd { get; private set; }
        public static Texture2D iconAlignToEndActive { get; private set; }
        public static Texture2D iconMoveToSurface { get; private set; }
        public static Texture2D iconMoveToSurfaceActive { get; private set; }
        public static Texture2D iconAuto { get; private set; }
        public static Texture2D iconAutoActive { get; private set; }
        public static Texture2D iconSpline { get; private set; }
        public static Texture2D iconSplineLight { get; private set; }
        public static Texture2D iconCreateSpline { get; private set; }
        public static Texture2D iconCreateSplineLight { get; private set; }
        public static Texture2D iconSplineObject { get; private set; }
        public static Texture2D iconSplineConnector { get; private set; }
        public static Texture2D iconMinimize { get; private set; }
        public static Texture2D iconPlus { get; private set; }
        public static Texture2D iconMinimizeBlack { get; private set; }
        public static Texture2D iconPlusBlack { get; private set; }
        public static Texture2D iconHandleMirrored { get; private set; }
        public static Texture2D iconHandleMirroredLight { get; private set; }
        public static Texture2D iconHandleContinuous { get; private set; }
        public static Texture2D iconHandleContinuousLight { get; private set; }
        public static Texture2D iconHandleBroken { get; private set; }
        public static Texture2D iconHandleBrokenLight { get; private set; }
        public static Texture2D iconHandleAuto { get; private set; }
        public static Texture2D iconHandleAutoLight { get; private set; }
        public static Texture2D iconMenuControlPanel { get; private set; }
        public static Texture2D iconMenuControlPanelLight { get; private set; }
        public static Texture2D iconMenuSettings { get; private set; }
        public static Texture2D iconMenuSettingsLight { get; private set; }
        public static Texture2D iconMenuInfo { get; private set; }
        public static Texture2D iconMenuInfoLight { get; private set; }
        public static Texture2D iconConstrined { get; private set; }
        public static Texture2D iconNotConstrined { get; private set; }
        public static Texture2D iconJoin { get; private set; }
        public static Texture2D iconPlane { get; private set; }
        public static Texture2D iconPlaneLight { get; private set; }
        public static Texture2D iconGrid { get; private set; }
        public static Texture2D iconGridLight { get; private set; }
        public static Texture2D iconGeneral { get; private set; }
        public static Texture2D iconGeneralActive { get; private set; }
        public static Texture2D iconMirror { get; private set; }
        public static Texture2D iconMirrorActive { get; private set; }
        public static Texture2D iconCurve { get; private set; }
        public static Texture2D iconCurveActive { get; private set; }
        public static Texture2D iconLoop { get; private set; }
        public static Texture2D iconLoopActive { get; private set; }
        public static Texture2D iconNormals { get; private set; }
        public static Texture2D iconNormalsLight { get; private set; }
        public static Texture2D iconHide { get; private set; }
        public static Texture2D iconHideLight { get; private set; }
        public static Texture2D iconInfo { get; private set; }
        public static Texture2D iconInfoActive { get; private set; }
        public static Texture2D iconSettings { get; private set; }
        public static Texture2D iconReverse { get; private set; }
        public static Texture2D moveToPlaneIcon { get; private set; }
        public static Texture2D iconToCenter { get; private set; }
        public static Texture2D iconSelectSpline { get; private set; }
        public static Texture2D iconSelectAll { get; private set; }
        public static Texture2D iconNextControlPoint { get; private set; }
        public static Texture2D iconPrevControlPoint { get; private set; }
        public static Texture2D iconMove { get; private set; }
        public static Texture2D iconSplit { get; private set; }
        public static Texture2D iconLink { get; private set; }
        public static Texture2D iconUnlink { get; private set; }
        public static Texture2D iconAlign { get; private set; }
        public static Texture2D iconNoise { get; private set; }
        public static Texture2D iconNoiseActive { get; private set; }
        public static Texture2D iconDefault { get; private set; }
        public static Texture2D iconX { get; private set; }
        public static Texture2D iconExternalLink { get; private set; }
        public static Texture2D iconUpArrow { get; private set; }
        public static Texture2D iconDownArrow { get; private set; }
        public static Texture2D iconExport { get; private set; }
        public static Texture2D iconCopy { get; private set; }
        public static Texture2D iconPaste { get; private set; }
        public static Texture2D iconAlignTangents { get; private set; }
        public static Texture2D iconMagnet { get; private set; }
        public static Texture2D iconMagnetActive { get; private set; }
        public static Texture2D iconMagnetActive2 { get; private set; }

        public static Texture2D iconInfoMsg { get; private set; }
        public static Texture2D iconWarningMsg { get; private set; }
        public static Texture2D iconErrorMsg { get; private set; }

        public static Texture2D gScale95_100 { get; private set; }
        public static Texture2D gScale90_100 { get; private set; }
        public static Texture2D gScale80_100 { get; private set; }
        public static Texture2D gScale70_100 { get; private set; }
        public static Texture2D gScale60_100 { get; private set; }
        public static Texture2D gScale50_100 { get; private set; }
        public static Texture2D gScale40_100 { get; private set; }
        public static Texture2D gScale40_90 { get; private set; }
        public static Texture2D gScale30_100 { get; private set; }
        public static Texture2D gScale25_100 { get; private set; }
        public static Texture2D gScale20_100 { get; private set; }
        public static Texture2D gScale20_80 { get; private set; }
        public static Texture2D gScale15_100 { get; private set; }
        public static Texture2D gScale10_100 { get; private set; }
        public static Texture2D gScale10_80 { get; private set; }
        public static Texture2D gScale8_100 { get; private set; }
        public static Texture2D gScale7_100 { get; private set; }
        public static Texture2D gScale5_100 { get; private set; }
        public static Texture2D gScale3_100 { get; private set; }
        public static Texture2D gScale0_100 { get; private set; }
        public static Texture2D gScale0_80 { get; private set; }
        public static Texture2D gScale0_50 { get; private set; }
        public static Texture2D yellow100 { get; private set; }

        public static Texture2D empty { get; private set; }

        public static string textureFolderPath = $"{EHandleFolder.GetMainFolderPath()}/Core/Textures/";

        internal static void Init()
        {
            gScale95_100 = LoadImage("gScale95_100.png");
            gScale90_100 = LoadImage("gScale90_100.png");
            gScale80_100 = LoadImage("gScale80_100.png");
            gScale70_100 = LoadImage("gScale70_100.png");
            gScale60_100 = LoadImage("gScale60_100.png");
            gScale50_100 = LoadImage("gScale50_100.png");
            gScale40_100 = LoadImage("gScale40_100.png");
            gScale40_90 = LoadImage("gScale40_90.png");
            gScale30_100 = LoadImage("gScale30_100.png");
            gScale25_100 = LoadImage("gScale25_100.png");
            gScale20_100 = LoadImage("gScale20_100.png");
            gScale20_80 = LoadImage("gScale20_80.png");
            gScale15_100 = LoadImage("gScale15_100.png");
            gScale10_100 = LoadImage("gScale10_100.png");
            gScale10_80 = LoadImage("gScale10_80.png");
            gScale8_100 = LoadImage("gScale8_100.png");
            gScale7_100 = LoadImage("gScale7_100.png");
            gScale5_100 = LoadImage("gScale5_100.png");
            gScale3_100 = LoadImage("gScale3_100.png");
            gScale0_100 = LoadImage("gScale0_100.png");
            gScale0_80 = LoadImage("gScale0_80.png");
            gScale0_50 = LoadImage("gScale0_50.png");
            yellow100 = LoadImage("yellow100.png");

            iconAlignToEnd = LoadImage("alignToEndIcon.png");
            iconAlignToEndActive = LoadImage("alignToEndIcon_active.png");
            iconMoveToSurface = LoadImage("moveToSurfaceIcon.png");
            iconMoveToSurfaceActive = LoadImage("moveToSurfaceIcon_active.png");
            iconAuto = LoadImage("autoIcon.png");
            iconAutoActive = LoadImage("autoIcon_active.png");
            iconSpline = LoadImage("splineIcon.png");
            iconSplineLight = LoadImage("splineLightIcon.png");
            iconCreateSpline = LoadImage("createSplineIcon.png");
            iconCreateSplineLight = LoadImage("createSplineLightIcon.png");
            iconSplineObject = LoadImage("splineObjectIcon.png");
            iconSplineConnector = LoadImage("splineConnectorIcon.png");
            iconPlus = LoadImage("plusIcon.png");
            iconPlusBlack = LoadImage("plusBlackIcon.png");
            iconHandleMirrored = LoadImage("handleMirroredIcon.png");
            iconHandleMirroredLight = LoadImage("handleMirroredLightIcon.png");
            iconHandleContinuous = LoadImage("handleContinuousIcon.png");
            iconHandleContinuousLight = LoadImage("handleContinuousLightIcon.png");
            iconHandleBroken = LoadImage("handleBrokenIcon.png");
            iconHandleBrokenLight = LoadImage("handleBrokenLightIcon.png");
            iconHandleAuto = LoadImage("handleAutoIcon.png");
            iconHandleAutoLight = LoadImage("handleAutoLightIcon.png");
            iconMenuControlPanel = LoadImage("menuControlPanelIcon.png");
            iconMenuControlPanelLight = LoadImage("menuControlPanelLightIcon.png");
            iconMenuSettings = LoadImage("menuSettingsIcon.png");
            iconMenuSettingsLight = LoadImage("menuSettingsLightIcon.png");
            iconMenuInfo = LoadImage("menuInfoIcon.png");
            iconMenuInfoLight = LoadImage("menuInfoLightIcon.png");
            iconConstrined = LoadImage("constrainedIcon.png");
            iconNotConstrined = LoadImage("notConstrainedIcon.png");
            iconErrorMsg = LoadImage("errorMsgIcon.png");
            iconWarningMsg = LoadImage("warningMsgIcon.png");
            iconInfoMsg = LoadImage("infoMsgIcon.png");
            iconMove = LoadImage("moveIcon.png");
            iconNextControlPoint = LoadImage("nextControlPointIcon.png");
            iconPrevControlPoint = LoadImage("prevControlPointIcon.png");
            iconSelectSpline = LoadImage("selectCurveIcon.png");
            iconSelectAll = LoadImage("selectAllIcon.png");
            iconToCenter = LoadImage("toCenterIcon.png");
            iconJoin = LoadImage("joinIcon.png");
            iconPlane = LoadImage("planeIcon.png");
            iconPlaneLight = LoadImage("planeLightIcon.png");
            iconGrid = LoadImage("gridIcon.png");
            iconGridLight = LoadImage("gridLightIcon.png");
            iconGeneral = LoadImage("generalIcon.png");
            iconGeneralActive = LoadImage("generalIcon_active.png");
            iconMirror = LoadImage("mirrorIcon.png");
            iconMirrorActive = LoadImage("mirrorIcon_active.png");
            iconCurve = LoadImage("curveIcon.png");
            iconCurveActive = LoadImage("curveIcon_active.png");
            iconNoise = LoadImage("noiseIcon.png");
            iconNoiseActive = LoadImage("noiseIcon_active.png");
            iconLoop = LoadImage("loopIcon.png");
            iconLoopActive = LoadImage("loopIcon_active.png");
            iconNormals = LoadImage("normalsIcon.png");
            iconNormalsLight = LoadImage("normalsLightIcon.png");
            iconHide = LoadImage("hideIcon.png");
            iconHideLight = LoadImage("hideLightIcon.png");
            iconInfo = LoadImage("infoIcon.png");
            iconSettings = LoadImage("settingsIcon.png");
            iconInfoActive = LoadImage("infoIcon_active.png");
            iconReverse = LoadImage("reverseIcon.png");
            moveToPlaneIcon = LoadImage("moveToPlaneIcon.png");
            iconSplit = LoadImage("splitIcon.png");
            iconLink = LoadImage("linkIcon.png");
            iconUnlink = LoadImage("unlinkIcon.png");
            iconAlign = LoadImage("alignIcon.png");
            iconX = LoadImage("xIcon.png");
            iconExternalLink = LoadImage("externalLinkIcon.png");
            iconDefault = LoadImage("defaultIcon.png");
            iconUpArrow = LoadImage("upArrowIcon.png");
            iconDownArrow = LoadImage("downArrowIcon.png");
            iconExport = LoadImage("exportIcon.png");
            iconMinimize = LoadImage("minimizeIcon.png");
            iconMinimizeBlack = LoadImage("minimizeBlackIcon.png");
            iconCopy = LoadImage("copyIcon.png");
            iconPaste = LoadImage("pasteIcon.png");
            iconAlignTangents = LoadImage("alignTangentsIcon.png");
            iconMagnet = LoadImage("magnetIcon.png");
            iconMagnetActive = LoadImage("magnetIcon_active.png");
            iconMagnetActive2 = LoadImage("magnetIcon_active2.png");

            buttonDefault = LoadImage("defaultButton9s.png");
            buttonDefaultActive = LoadImage("defaultButton_active9s.png");
            buttonDefaultPressed = LoadImage("defaultButton_pressed9s.png");
            buttonSubMenu = LoadImage("subMenuButton9s.png");
            buttonSubMenuActive = LoadImage("subMenuButton_active9s.png");
            buttonSubMenuPressed = LoadImage("subMenuButton_pressed9s.png");
            buttonDefaultRed = LoadImage("defaultButtonRed9s.png");
            buttonDefaultRedPressed = LoadImage("defaultButtonRed_pressed9s.png");
            buttonDefaultWhite = LoadImage("defaultButtonWhite9s.png");
            buttonDefaultWhitePressed = LoadImage("defaultButtonWhite_pressed9s.png");
            buttonDefaultGreen = LoadImage("defaultButtonGreen9s.png");
            buttonDefaultGreenPressed = LoadImage("defaultButtonGreen_pressed9s.png");

            inputField = LoadImage("inputField9s.png");
            inputFieldActive = LoadImage("inputField_active9s.png");
            inputFieldDelayed = LoadImage("inputFieldDelayed9s.png");
            inputFieldDelayedActive = LoadImage("inputFieldDelayed_active9s.png");
            inputFieldPending = LoadImage("inputField_pending9s.png");

            empty = LoadImage("empty.png");

            popUpField = LoadImage("popUpField9s.png");
            infoBox = LoadImage("infoBox9s.png");
        }

        private static Texture2D LoadImage(string imageName)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{textureFolderPath}{imageName}");

            if (texture != null) 
                return texture;

            //When the user imports the package for the first time the AssetDatabase is not loaded and we need to preload the image.
            string dataPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
            byte[] bytes = File.ReadAllBytes($"{dataPath}{textureFolderPath}{imageName}");
            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            ImageConversion.LoadImage(texture, bytes, markNonReadable: true);
            preloadedTextures.Add(texture);
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }

        internal static void DestroyPreloadedTextures()
        {
            foreach(Texture2D tex in preloadedTextures) Object.DestroyImmediate(tex);
            preloadedTextures.Clear();
        }

        internal static int GetPreloadedTextureCount()
        {
            if (preloadedTextures == null)
                return 0;

            return preloadedTextures.Count;
        }
    }
}
