// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: LibraryGUIContent.cs
//
// Author: Mikael Danielsson
// Date Created: 08-12-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;

using SplineArchitect.Utility;

namespace SplineArchitect.Libraries
{
    public class LibraryGUIContent
    {
        //Icons
        public static GUIContent iconAlignToEnd { get; private set; }
        public static GUIContent iconAlignToEndActive { get; private set; }
        public static GUIContent iconMoveToSurface { get; private set; }
        public static GUIContent iconProjectToSurface { get; private set; }
        public static GUIContent iconProjectToSurfaceInactive { get; private set; }
        public static GUIContent iconProjectToSurfaceActive { get; private set; }
        public static GUIContent iconAuto { get; private set; }
        public static GUIContent iconAutoActive { get; private set; }
        public static GUIContent iconCreateSpline { get; private set; }
        public static GUIContent iconCreateSplineLight { get; private set; }
        public static GUIContent iconMaximize { get; private set; }
        public static GUIContent iconMinimize { get; private set; }
        public static GUIContent iconMaximizeBlack { get; private set; }
        public static GUIContent iconMinimizeBlack { get; private set; }
        public static GUIContent iconMenuControlPanel { get; private set; }
        public static GUIContent iconMenuSettings { get; private set; }
        public static GUIContent iconMenuInfo { get; private set; }
        public static GUIContent iconConstrained { get; private set; }
        public static GUIContent iconNotConstrained { get; private set; }
        public static GUIContent iconGrid { get; private set; }
        public static GUIContent iconGeneral { get; private set; }
        public static GUIContent iconGeneralActive { get; private set; }
        public static GUIContent iconMirrorDeformation { get; private set; }
        public static GUIContent iconMirrorDeformationActive { get; private set; }
        public static GUIContent iconMirrorConnector { get; private set; }
        public static GUIContent iconMirrorConnectorActive { get; private set; }
        public static GUIContent iconCurve { get; private set; }
        public static GUIContent iconCurveActive { get; private set; }
        public static GUIContent iconLoop { get; private set; }
        public static GUIContent iconLoopActive { get; private set; }
        public static GUIContent iconNormals { get; private set; }
        public static GUIContent iconHide { get; private set; }
        public static GUIContent iconAdd { get; private set; }
        public static GUIContent iconInfo { get; private set; }
        public static GUIContent iconInfoActive { get; private set; }
        public static GUIContent iconSettings { get; private set; }
        public static GUIContent iconReverse { get; private set; }
        public static GUIContent iconMoveSegmentToPlane { get; private set; }
        public static GUIContent iconToCenter { get; private set; }
        public static GUIContent iconSelectSpline { get; private set; }
        public static GUIContent iconSelectAll { get; private set; }
        public static GUIContent iconSelectLayer { get; private set; }
        public static GUIContent iconNextControlPoint { get; private set; }
        public static GUIContent iconPrevControlPoint { get; private set; }
        public static GUIContent iconMove { get; private set; }
        public static GUIContent iconSplit { get; private set; }
        public static GUIContent iconJoin { get; private set; }
        public static GUIContent iconLink { get; private set; }
        public static GUIContent iconUnlink { get; private set; }
        public static GUIContent iconAlign { get; private set; }
        public static GUIContent iconNoise { get; private set; }
        public static GUIContent iconNoiseActive { get; private set; }
        public static GUIContent iconDefault { get; private set; }
        public static GUIContent iconRemove { get; private set; }
        public static GUIContent iconClose { get; private set; }
        public static GUIContent iconUpArrow { get; private set; }
        public static GUIContent iconDownArrow { get; private set; }
        public static GUIContent iconExport { get; private set; }
        public static GUIContent iconCopy { get; private set; }
        public static GUIContent iconPaste { get; private set; }
        public static GUIContent iconAlignTangents { get; private set; }
        public static GUIContent iconMagnet { get; private set; }
        public static GUIContent iconMagnetActive { get; private set; }
        public static GUIContent iconMagnetActive2 { get; private set; }

        //Info messages
        public static GUIContent infoMsgUpdateType { get; private set; }
        public static GUIContent infoMsgDeformationType { get; private set; }
        public static GUIContent infoMsgNormalResolution { get; private set; }
        public static GUIContent infoMsgPositionResolution { get; private set; }
        public static GUIContent infoMsgSplineResolution { get; private set; }
        public static GUIContent infoMsgMeshData { get; private set; }
        public static GUIContent infoMsgSplineData { get; private set; }
        public static GUIContent infoMsgComponentData { get; private set; }
        public static GUIContent infoMsgSplineComponentMode { get; private set; }
        public static GUIContent infoMsgMeshMode { get; private set; }
        public static GUIContent infoMsgSplineDeformationModePrefab { get; private set; }
        public static GUIContent infoMsgSOComponentMode { get; private set; }
        public static GUIContent infoMsgZRotationNoneStatic { get; private set; }

        //Warning messages
        public static GUIContent warningMsgUseUnityCalculatedNormals { get; private set; }
        public static GUIContent warningMsgResourceCreateOptions { get; private set; }
        public static GUIContent warningMsgCantCloneWithNoChildren { get; private set; }
        public static GUIContent warningMsgCantDisconnectOriginClone { get; private set; }
        public static GUIContent warningMsgCanOnlyCloneWithTypeDeformation { get; private set; }
        public static GUIContent warningMsgCantRandomizeWithNoChildren { get; private set; }
        public static GUIContent warningMsgCanOnlyRandomizeWithTypeDeformation { get; private set; }
        public static GUIContent warningMsgCantDoRealtimeDeformation { get; private set; }
        public static GUIContent warningMsgNoLayerPalleteAssigned { get; private set; }
        public static GUIContent warningMsgComponentOptionIsRedundant { get; private set; }
        public static GUIContent warningMsgComponentOptionPrefab { get; private set; }

        //Error messages
        public static GUIContent errorMsgGenerateMeshRuntime { get; private set; }
        public static GUIContent errorMsgStaticSO { get; private set; }

        //Texts
        public static GUIContent textDeformTerrain { get; private set; }
        public static GUIContent textRevertTerrain { get; private set; }
        public static GUIContent textEnableTerrainDeformation { get; private set; }
        public static GUIContent textDisableTerrainDeformation { get; private set; }
        public static GUIContent textBakeTerrainDeformation { get; private set; }
        public static GUIContent textUserManual { get; private set; }
        public static GUIContent textDocumenation { get; private set; }
        public static GUIContent textDiscord { get; private set; }
        public static GUIContent textPaintTerrain { get; private set; }
        public static GUIContent textFillTerrain { get; private set; }
        public static GUIContent textCreateLayer { get; private set; }
        public static GUIContent textDeleteLayers { get; private set; }
        public static GUIContent textInstantaite { get; private set; }
        public static GUIContent textRemoveAll { get; private set; }
        public static GUIContent textSet { get; private set; }
        public static GUIContent textClone { get; private set; }
        public static GUIContent textDisconnect { get; private set; }
        public static GUIContent textCloneChildren { get; private set; }
        public static GUIContent textDeleteClones { get; private set; }
        public static GUIContent textResetValues { get; private set; }
        public static GUIContent textApplyToSelected { get; private set; }
        public static GUIContent textApplyToChildren { get; private set; }
        public static GUIContent textCreateConnection { get; private set; }
        public static GUIContent textRemoveConnections { get; private set; }
        public static GUIContent textCopyLayers { get; private set; }
        public static GUIContent textPasteLayers { get; private set; }
        public static GUIContent textSplineObjectComponent { get; private set; }
        public static GUIContent textSplineComponent { get; private set; }
        public static GUIContent textSplineConnectorComponent { get; private set; }

        //Other
        public static GUIContent minimize { get; private set; }
        public static GUIContent maximize { get; private set; }
        public static GUIContent empty { get; private set; }
        public static List<GUIContent> noiseTypes = new List<GUIContent>();

        internal static void Init()
        {
            //Icons
            iconAlignToEnd = new GUIContent(LibraryTexture.iconAlignToEnd, "Align to end of spline");
            iconMoveToSurface = new GUIContent(LibraryTexture.iconMoveToSurface, "Move to surface");
            iconAlignToEndActive = new GUIContent(LibraryTexture.iconAlignToEndActive, "Align to start of spline");
            iconProjectToSurface = new GUIContent(LibraryTexture.iconMoveToSurface, "Enable project to surface");
            iconProjectToSurfaceInactive = new GUIContent(LibraryTexture.iconMoveToSurface, "Enable project to surface");
            iconProjectToSurfaceActive = new GUIContent(LibraryTexture.iconMoveToSurfaceActive, "Disable project to surface");
            iconAuto = new GUIContent(LibraryTexture.iconAuto, "Auto type - Automatically switch between types follower and deformation");
            iconAutoActive = new GUIContent(LibraryTexture.iconAutoActive, "Auto type - Automatically switch between types follower and deformation");
            iconCreateSpline = new GUIContent(LibraryTexture.iconCreateSpline, "Spline Architect - Create Spline or Control Point on selected spline");
            iconCreateSplineLight = new GUIContent(LibraryTexture.iconCreateSplineLight, "Spline Architect - Create Spline or Control Point on selected spline");
            iconMaximize = new GUIContent(LibraryTexture.iconPlus, "Maximize");
            iconMaximizeBlack = new GUIContent(LibraryTexture.iconPlusBlack, "Maximize");
            iconMove = new GUIContent(LibraryTexture.iconMove, "Move activeInstance");
            iconSplit = new GUIContent(LibraryTexture.iconSplit, "Split spline");
            iconJoin = new GUIContent(LibraryTexture.iconJoin, "Join selected splines");
            iconLink = new GUIContent(LibraryTexture.iconLink, "Link to closest control point or Spline Connector");
            iconUnlink = new GUIContent(LibraryTexture.iconUnlink, "Unlink control point");
            iconNextControlPoint = new GUIContent(LibraryTexture.iconNextControlPoint, "Go to next control point");
            iconPrevControlPoint = new GUIContent(LibraryTexture.iconPrevControlPoint, "Go to prev control point");
            iconSelectSpline = new GUIContent(LibraryTexture.iconSelectSpline, "Select and center spline transform");
            iconSelectAll = new GUIContent(LibraryTexture.iconSelectAll, "Select all anchors");
            iconSelectLayer = new GUIContent(LibraryTexture.iconSelectSpline, "Select layer");
            iconToCenter = new GUIContent(LibraryTexture.iconToCenter, "To center");
            iconGrid = new GUIContent(LibraryTexture.iconGrid, "Enable grid");
            iconGeneral = new GUIContent(LibraryTexture.iconGeneral, "General");
            iconGeneralActive = new GUIContent(LibraryTexture.iconGeneralActive, "General");
            iconMirrorDeformation = new GUIContent(LibraryTexture.iconMirror, "Enable mirror deformation");
            iconMirrorDeformationActive = new GUIContent(LibraryTexture.iconMirrorActive, "Disable mirror deformation");
            iconMirrorConnector = new GUIContent(LibraryTexture.iconMirror, "Enable mirror spline connector");
            iconMirrorConnectorActive = new GUIContent(LibraryTexture.iconMirrorActive, "Disable mirror spline connector");
            iconLoop = new GUIContent(LibraryTexture.iconLoop, "Enable loop");
            iconLoopActive = new GUIContent(LibraryTexture.iconLoopActive, "Disable loop");
            iconNormals = new GUIContent(LibraryTexture.iconNormals, "Show normals");
            iconHide = new GUIContent(LibraryTexture.iconHide, "Hide all unselected splines");
            iconAdd = new GUIContent(LibraryTexture.iconPlus, "Add");
            iconNoise = new GUIContent(LibraryTexture.iconNoise, "Noise effects");
            iconNoiseActive = new GUIContent(LibraryTexture.iconNoiseActive, "Noise effects");
            iconDefault = new GUIContent(LibraryTexture.iconDefault, "Set to default value");
            iconInfo = new GUIContent(LibraryTexture.iconInfo, "Info");
            iconInfoActive = new GUIContent(LibraryTexture.iconInfoActive, "Info");
            iconSettings = new GUIContent(LibraryTexture.iconSettings, "Settings");
            iconCurve = new GUIContent(LibraryTexture.iconCurve, "General");
            iconCurveActive = new GUIContent(LibraryTexture.iconCurveActive, "General");
            iconReverse = new GUIContent(LibraryTexture.iconReverse, "Reverse control points");
            iconMoveSegmentToPlane = new GUIContent(LibraryTexture.moveToPlaneIcon, "Move to spline plane");
            iconConstrained = new GUIContent(LibraryTexture.iconConstrined, "Disabled constrained");
            iconNotConstrained = new GUIContent(LibraryTexture.iconNotConstrined, "Enable constrained");
            iconAlign = new GUIContent(LibraryTexture.iconAlign, "Align selected control points");
            iconRemove= new GUIContent(LibraryTexture.iconX, "Remove");
            iconClose = new GUIContent(LibraryTexture.iconX, "Close");
            iconUpArrow = new GUIContent(LibraryTexture.iconUpArrow, "Move up");
            iconDownArrow = new GUIContent(LibraryTexture.iconDownArrow, "Move down");
            iconExport = new GUIContent(LibraryTexture.iconExport, "Export mesh");
            iconMinimize = new GUIContent(LibraryTexture.iconMinimize, "Minimize");
            iconMinimizeBlack = new GUIContent(LibraryTexture.iconMinimizeBlack, "Minimize");
            iconCopy = new GUIContent(LibraryTexture.iconCopy, "Copy");
            iconPaste = new GUIContent(LibraryTexture.iconPaste, "Paste");
            iconAlignTangents = new GUIContent(LibraryTexture.iconAlignTangents, "Align tangents");
            iconMagnet = new GUIContent(LibraryTexture.iconMagnet, "Enable control point snapping");
            iconMagnetActive = new GUIContent(LibraryTexture.iconMagnetActive, "Enable spline object snapping");
            iconMagnetActive2 = new GUIContent(LibraryTexture.iconMagnetActive2, "Disable snapping");

            //Info messages
            infoMsgMeshData = new GUIContent(LibraryTexture.iconInfoMsg, "Size of all deformed meshes on the spline.");
            infoMsgSplineData = new GUIContent(LibraryTexture.iconInfoMsg, "Size of all spline data.");
            infoMsgComponentData = new GUIContent(LibraryTexture.iconInfoMsg, "Size of the spline and spline object components in memory and disk space.");
            infoMsgSplineResolution = new GUIContent(LibraryTexture.iconInfoMsg, "By default, retrieving positions along a Bezier curve results in uneven spacing due to the bezier curve's nature. \n \n" +
                                                                                 "Spline Architect includes a smoothing system designed to counteract this, ensuring even distribution of positions across the spline, " +
                                                                                 "which prevents deformations from becoming squeezed or elongated. \n \n" +
                                                                                 "Adjusting the resolution higher will enhance the smoothness of deformations but at the cost of increased memory usage, " +
                                                                                 "without negatively impacting performance.");
            infoMsgPositionResolution = new GUIContent(LibraryTexture.iconInfoMsg, "The resolution of cached positions per 100 meters. You can turn off cached positions, but deforming meshes will require more performance.");
            infoMsgNormalResolution = new GUIContent(LibraryTexture.iconInfoMsg, "The resolution of cached normals per 100 meters.");
            infoMsgUpdateType = new GUIContent(LibraryTexture.iconInfoMsg,"Controls when the spline updates its deformations and followers. " +
                                                                          "Use 'Late Update' when generating objects on the spline, but in most cases 'Update' is recommended.");
            infoMsgDeformationType = new GUIContent(LibraryTexture.iconInfoMsg, "Static splines do not use cached normals and, because of this, consume less memory. " +
                                                                                "They can also be easier to work with since their normals do not change in the same way as those of a dynamic spline. \n \n" +
                                                                                "However, dynamic splines are capable of handling more advanced shapes, such as loops.");
            infoMsgSplineComponentMode = new GUIContent(LibraryTexture.iconInfoMsg, "Options for how the spline component is handled during runtime. \n \n" +
                                                                                    "Remove from build: \nRemoves the component during the build process. They won't be in the final game (not working with splines inside a prefab). \n \n" +
                                                                                    "Inactivate after scene load: \nDisables the component after scene load and deletes all stored native data. \n \n" +
                                                                                    "Active: \nThe component is active and can be used with scripts in your built game.");
            infoMsgMeshMode = new GUIContent(LibraryTexture.iconInfoMsg, "Options for how to handle the deformed mesh. \n \n" +
                                                                         "Generate: \nGenerates the mesh after scene load. " +
                                                                         "Note: The spline and spline object components must be in your built game for this to work, and the mesh needs to have Read/Write access. \n \n" +
                                                                         "Save in scene: \nSaves the mesh in the scene file. \n \n" +
                                                                         "Save in build: \nSaves the mesh only in your built game (scene file in your project will take less disk space). \n \n" +
                                                                         "Do nothing: \nGenerates the mesh only in editor mode. This can be useful if you want to manage mesh generation yourself.");
            infoMsgSplineDeformationModePrefab = new GUIContent(LibraryTexture.iconInfoMsg, "Prefabs can only be generated.");
            infoMsgSOComponentMode = new GUIContent(LibraryTexture.iconInfoMsg, "How the spline object component is handled during runtime. \n \n" +
                                                                                "Remove from build: \nRemoves the component during the build process (not working with spline objects inside a prefab). \n \n" +
                                                                                "Inactivate after scene load: \nDisables the component after scene load. \n \n" +
                                                                                "Active: \nThe component is active and can be used with scripts in your built game.");
            infoMsgZRotationNoneStatic = new GUIContent(LibraryTexture.iconInfoMsg,"This segment is not static, so the rotation tool is enabled. Primarily use the rotation tool to rotate the segment around the Z axis. " +
                                                                                   "\n\nBut the rotation tool is limited to 180 degrees in each direction, so use Z Rotation when you need a larger rotation.");

            //Warning messages
            warningMsgUseUnityCalculatedNormals = new GUIContent(LibraryTexture.iconWarningMsg, "Unity calculated normals are recommended when projecting the mesh onto a surface.");
            warningMsgCantCloneWithNoChildren = new GUIContent(LibraryTexture.iconWarningMsg, "No children to clone. You need to attach children to this GameObject in order to clone them. " +
                                                                                              "If you want to clone this object, create a new empty GameObject and attach this object to it.");
            warningMsgCantDisconnectOriginClone = new GUIContent(LibraryTexture.iconWarningMsg, "This GameObject cannot be disconnected because other clones depend on it as the origin clone.");
            warningMsgCanOnlyCloneWithTypeDeformation = new GUIContent(LibraryTexture.iconWarningMsg, "Cloning is only possible when using type 'Deformation'.");
            warningMsgCanOnlyRandomizeWithTypeDeformation = new GUIContent(LibraryTexture.iconWarningMsg, "Randomizing offsets to children is only possible using ControlHandle Deformation.");
            warningMsgCantRandomizeWithNoChildren = new GUIContent(LibraryTexture.iconWarningMsg, "No children to randomize. You need to attach children to this GameObject in order to randomize them.");
            warningMsgCantDoRealtimeDeformation = new GUIContent(LibraryTexture.iconWarningMsg, "Cannot perform real-time deformations or generate the mesh in Play Mode or in a built application. " +
                                                                                                "Enable Read/Write access on the original mesh if you want to allow this.");
            warningMsgNoLayerPalleteAssigned = new GUIContent(LibraryTexture.iconWarningMsg, "No terrain layers are assigned. You need to assign a one or more terrain layers for the terrain.");
            warningMsgComponentOptionIsRedundant = new GUIContent(LibraryTexture.iconWarningMsg, "This spline object is set to 'Remove from build'. Choosing any other component option for the spline object is redundant. \n\n" +
                                                                                                 "To animate this spline object, ensure the spline component is set to 'Active'.");
            warningMsgComponentOptionPrefab = new GUIContent(LibraryTexture.iconWarningMsg, "This component is part of a nested prefab and not attached to the root prefab GameObject. " +
                                                                                            "As a result, the component will mirror the root GameObject's settings (hideFlags).");

            //Error messages
            errorMsgGenerateMeshRuntime = new GUIContent(LibraryTexture.iconErrorMsg, "Can't use 'Remove from build' on the spline or spline object when generating a mesh at runtime.");
            errorMsgStaticSO = new GUIContent(LibraryTexture.iconErrorMsg, "The Deformation option on the spline is currently set to Generate. " +
                                                                           "Static deformations can't be generated during scene load and need to be saved either in the build or in the scene file. \n\n" +
                                                                           "Please set the Deformation option on the spline to Save in Build or Save in Scene.");

            //Texts
            textDeformTerrain = new GUIContent("Deform terrain");
            textRevertTerrain = new GUIContent("Revert terrain");
            textEnableTerrainDeformation = new GUIContent("Start deforming");
            textDisableTerrainDeformation = new GUIContent("Stop deforming");
            textBakeTerrainDeformation = new GUIContent("Bake", "Applies the spline deformation permanently to the terrain. Changes cannot be undone afterward.");
            textUserManual = new GUIContent("User manual", LibraryTexture.iconExternalLink, "https://splinearchitect.com/user_manual");
            textDocumenation = new GUIContent("Documentation", LibraryTexture.iconExternalLink, "https://splinearchitect.com");
            textDiscord = new GUIContent("Discord", LibraryTexture.iconExternalLink, "https://discord.gg/uDyCeGKff7");
            textPaintTerrain = new GUIContent("Paint", "Paint terrain");
            textFillTerrain = new GUIContent("Fill", "Fill terrain");
            textCreateLayer = new GUIContent("Create layer");
            textDeleteLayers = new GUIContent("Delete layers");
            textInstantaite = new GUIContent("Instantiate");
            textRemoveAll = new GUIContent("Remove all");
            textSet = new GUIContent("Set");
            textClone = new GUIContent("Clone");
            textDisconnect = new GUIContent("Disconnect");
            textCloneChildren = new GUIContent("Clone children");
            textDeleteClones = new GUIContent("Delete clones");
            textResetValues = new GUIContent("Reset values");
            textApplyToSelected = new GUIContent("Apply to selected");
            textApplyToChildren = new GUIContent("Apply to children");
            textCreateConnection = new GUIContent("Create");
            textRemoveConnections = new GUIContent("Remove all");
            textCopyLayers = new GUIContent("Copy layers");
            textPasteLayers = new GUIContent("Paste layers");

            //Other
            empty = new GUIContent(LibraryTexture.empty, "");

            noiseTypes.Clear();
            foreach (string s in Enum.GetNames(typeof(NoiseType)))
            {
                noiseTypes.Add(new GUIContent(EConversionUtility.CapitalizeString(s)));
            }

            ReassureCustomEditorData();
        }

        internal static void ReassureCustomEditorData()
        {
            if (textSplineObjectComponent == null)
                textSplineObjectComponent = new GUIContent(
                    "A component used by Spline Architect.\n\n" +
                    "This component is automatically added when GameObjects are attached to a spline in the hierarchy window. " +
                    "It can also be manually added to any GameObject for pre-configuration."
                );


            if (textSplineComponent == null)
                textSplineComponent = new GUIContent(
                    "A component used by Spline Architect.\n\n" +
                    "All spline settings are available in the Spline Architect control panel. " +
                    "Open the control panel by clicking the Control Panel button in the Spline Architect toolbar."
                );


            if (textSplineConnectorComponent == null)
                textSplineConnectorComponent = new GUIContent(
                    "A component used by Spline Architect.\n\n" +
                    "Control points from any spline can be connected to this component. " +
                    "All connected splines will update when you move this GameObject."
                );

        }
    }
}
