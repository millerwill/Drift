// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: GUIStyleLibrary.cs
//
// Author: Mikael Danielsson
// Date Created: 18-06-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEditor;

using SplineArchitect.Ui;

namespace SplineArchitect.Libraries
{
    public static class LibraryGUIStyle
    {
        //General
        public static GUIStyle specificX { get; private set; }
        public static GUIStyle specificYZ { get; private set; }
        public static GUIStyle specificFromToX { get; private set; }
        public static GUIStyle specificFromToYZ { get; private set; }
        public static GUIStyle popUpFieldSmallText { get; private set; }
        public static GUIStyle separatorWhite { get; private set; }
        public static GUIStyle infoIcon { get; private set; }
        public static GUIStyle empty { get; private set; }
        public static GUIStyle scrollbarStyle { get; private set; }
        public static GUIStyle componentInfo { get; private set; }
        public static GUIStyle pressEnter { get; private set; }

        //Buttons
        public static GUIStyle buttonDefault { get; private set; }
        public static GUIStyle buttonDefaultGreen { get; private set; }
        public static GUIStyle buttonDefaultRed { get; private set; }
        public static GUIStyle buttonDefaultWhite { get; private set; }
        public static GUIStyle buttonDefaultActive { get; private set; }
        public static GUIStyle buttonDefaultMiddleLeft { get; private set; }
        public static GUIStyle buttonDefaultMiddleLeftActive { get; private set; }
        public static GUIStyle buttonSubMenu { get; private set; }
        public static GUIStyle buttonSubMenuActive { get; private set; }
        public static GUIStyle buttonSubMenu2 { get; private set; }

        //Texts
        public static GUIStyle textHeader { get; private set; }
        public static GUIStyle textHeaderBlack { get; private set; }
        public static GUIStyle textSection { get; private set; }
        public static GUIStyle textSubSection { get; private set; }
        public static GUIStyle textLayerSection { get; private set; }
        public static GUIStyle textDefault { get; private set; }
        public static GUIStyle textDefaultBlack { get; private set; }
        public static GUIStyle textSmall { get; private set; }
        public static GUIStyle textNoWdith { get; private set; }
        public static GUIStyle textFieldNoWidth { get; private set; }
        public static GUIStyle textFieldDelayedNoWidth { get; private set; }
        public static GUIStyle textFieldPendingNoWidth { get; private set; }
        public static GUIStyle textField { get; private set; }
        public static GUIStyle textFieldSmall { get; private set; }
        public static GUIStyle textFieldVerySmall { get; private set; }
        public static GUIStyle textSceneView { get; private set; }

        //Lines
        public static GUIStyle lineWhite { get; private set; }
        public static GUIStyle lineYellowThick { get; private set; }
        public static GUIStyle lineBlackThick { get; private set; }
        public static GUIStyle lineYellow { get; private set; }
        public static GUIStyle lineBlack { get; private set; }
        public static GUIStyle lineGrey80 { get; private set; }
        public static GUIStyle lineGrey40 { get; private set; }
        public static GUIStyle lineGrey30 { get; private set; }
        public static GUIStyle lineGrey20 { get; private set; }

        //Backgrounds
        public static GUIStyle backgroundHeader { get; private set; }
        public static GUIStyle backgroundBottomMenu { get; private set; }
        public static GUIStyle backgroundNoSplineSelected { get; private set; }
        public static GUIStyle backgroundToolbar { get; private set; }
        public static GUIStyle backgroundSection { get; private set; }
        public static GUIStyle backgroundSubSection { get; private set; }
        public static GUIStyle backgroundLayerSection { get; private set; }
        public static GUIStyle backgroundItem2 { get; private set; }
        public static GUIStyle backgroundItem1 { get; private set; }
        public static GUIStyle backgroundSelectedLayerHeader { get; private set; }
        public static GUIStyle backgroundSelectedLayer { get; private set; }
        public static GUIStyle backgroundMainLayerButtons { get; private set; }

        //Need to instalize like this. LibraryGUIStyle is dependent on different textures that needs to be created first.
        internal static void Init()
        {
            //General
            specificX = CreateTextStyle(Color.white, new RectOffset(6, 4, 2, 2), new RectOffset(0, 0, 0, 0), 13, 15);
            specificYZ = CreateTextStyle(Color.white, new RectOffset(6, 4, 2, 2), new RectOffset(0, 0, 0, 0), 13, 15);
            specificFromToX = CreateTextStyle(Color.white, new RectOffset(6, 4, 2, 2), new RectOffset(0, 0, 0, 0), 13, 15);
            specificFromToYZ = CreateTextStyle(Color.white, new RectOffset(6, 4, 2, 2), new RectOffset(10, 0, 0, 0), 13, 15);
            empty = new GUIStyle();
            scrollbarStyle = CreateScrollBar();
            separatorWhite = CreateBoxStyle(LibraryTexture.gScale80_100, 16, 4, new RectOffset(0, 0, 3, 0));
            pressEnter = CreateInfoBox(28, 12, 9);

            //Backgrounds
            backgroundHeader = CreateBackgroundStyle(LibraryTexture.gScale95_100, 18);
            backgroundSection = CreateBackgroundStyle(LibraryTexture.gScale80_100, 14);
            backgroundSubSection = CreateBackgroundStyle(LibraryTexture.gScale20_100, 14);
            backgroundLayerSection = CreateBackgroundStyle(LibraryTexture.gScale15_100, 0);
            backgroundToolbar = CreateBackgroundStyle(LibraryTexture.gScale40_100, 23);
            backgroundBottomMenu = CreateBackgroundStyle(LibraryTexture.gScale15_100, 18);
            backgroundItem1 = CreateBackgroundStyle(LibraryTexture.gScale8_100, WindowBase.itemHeight);
            backgroundItem2 = CreateBackgroundStyle(LibraryTexture.gScale7_100, WindowBase.itemHeight);
            backgroundSelectedLayerHeader = CreateBackgroundStyle(LibraryTexture.yellow100, WindowBase.itemHeight);
            backgroundSelectedLayer = CreateBackgroundStyle(LibraryTexture.gScale15_100, WindowBase.itemHeight);
            backgroundMainLayerButtons = CreateBackgroundStyle(LibraryTexture.gScale30_100, WindowBase.itemHeight);

            //Lines
            lineWhite = CreateBoxStyle(LibraryTexture.gScale95_100, 0, 0, new RectOffset(0, 0, 0, 0));
            lineYellowThick = CreateBackgroundStyle(LibraryTexture.yellow100, 4);
            lineBlackThick = CreateBackgroundStyle(LibraryTexture.gScale5_100, 4);
            lineYellow = CreateBoxStyle(LibraryTexture.yellow100, 0, 0, new RectOffset(0, 0, 0, 0));
            lineBlack = CreateBoxStyle(LibraryTexture.gScale0_100, 0, 0, new RectOffset(0, 0, 0, 0));
            lineGrey80 = CreateBoxStyle(LibraryTexture.gScale80_100, 0, 0, new RectOffset(0, 0, 0, 0));
            lineGrey40 = CreateBoxStyle(LibraryTexture.gScale40_100, 0, 0, new RectOffset(0, 0, 0, 0));
            lineGrey30 = CreateBoxStyle(LibraryTexture.gScale30_100, 0, 0, new RectOffset(0, 0, 0, 0));
            lineGrey20 = CreateBoxStyle(LibraryTexture.gScale20_100, 0, 0, new RectOffset(0, 0, 0, 0));

            //Texts
            textHeader = CreateTextStyle(Color.white, new RectOffset(6, 4, 2, 2), new RectOffset(0, 0, 0, 0), 12);
            textHeaderBlack = CreateTextStyle(Color.black, new RectOffset(6, 4, -5, 0), new RectOffset(0, 0, 0, 0), 12);
            textSection = CreateTextStyle(new Color(0, 0, 0, 1), new RectOffset(6, 4, -8, 0), new RectOffset(0, 0, 0, 0), 10);
            textSubSection = CreateTextStyle(new Color(1, 1, 1, 1), new RectOffset(6, 4, -8, 0), new RectOffset(0, 0, 0, 0), 9);
            textLayerSection = CreateTextStyle(new Color(1, 1, 1, 1), new RectOffset(6, 4, 0, 0), new RectOffset(0, 0, 3, 1), 9, 0, 10);
            textDefault = CreateTextStyle(Color.white, new RectOffset(6, 4, 0, 2), new RectOffset(0, 0, 0, 0), 12);
            textDefaultBlack = CreateTextStyle(Color.black, new RectOffset(6, 4, 0, 2), new RectOffset(0, 0, 0, 0), 12);
            textNoWdith = CreateTextStyle(Color.white, new RectOffset(0, 0, 0, 2), new RectOffset(0, 0, 0, 0), 12);
            textSmall = CreateTextStyle(Color.white, new RectOffset(6, 4, 0, 2), new RectOffset(0, 0, 0, 0), 10);
            textSceneView = CreateSceneViewText(18, new RectOffset(2, 1, 2, 3));

            //Fields
            textFieldNoWidth = CreateTextField(0, 18, new RectOffset(2, 2, 2, 2), LibraryTexture.inputField, LibraryTexture.inputFieldActive);
            textFieldDelayedNoWidth = CreateTextField(0, 18, new RectOffset(2, 2, 2, 2), LibraryTexture.inputFieldDelayed, LibraryTexture.inputFieldDelayedActive);
            textFieldPendingNoWidth = CreateTextField(0, 18, new RectOffset(2, 2, 2, 2), LibraryTexture.inputFieldPending, LibraryTexture.inputFieldPending);
            textField = CreateTextField(50, 18, new RectOffset(2, 2, 2, 2), LibraryTexture.inputField, LibraryTexture.inputFieldActive);
            textFieldSmall = CreateTextField(38, 18, new RectOffset(2, 2, 2, 2), LibraryTexture.inputField, LibraryTexture.inputFieldActive);
            textFieldVerySmall = CreateTextField(26, 18, new RectOffset(2, 2, 2, 2), LibraryTexture.inputField, LibraryTexture.inputFieldActive);

            //Others
            popUpFieldSmallText = CreatePopUpFiled();
            infoIcon = CreateInfoMsgIcon();

            //Buttons
            buttonDefault = CreateButton(LibraryTexture.buttonDefault, LibraryTexture.buttonDefaultPressed, new RectOffset(2, 2, 2, 2), TextAnchor.MiddleCenter);
            buttonDefaultGreen = CreateButton(LibraryTexture.buttonDefaultGreen, LibraryTexture.buttonDefaultGreenPressed, new RectOffset(2, 2, 2, 2), TextAnchor.MiddleCenter);
            buttonDefaultRed = CreateButton(LibraryTexture.buttonDefaultRed, LibraryTexture.buttonDefaultRedPressed, new RectOffset(2, 2, 2, 2), TextAnchor.MiddleCenter);
            buttonDefaultWhite = CreateButton(LibraryTexture.buttonDefaultWhite, LibraryTexture.buttonDefaultWhitePressed, new RectOffset(2, 2, 2, 2), TextAnchor.MiddleCenter);
            buttonDefaultActive = CreateButton(LibraryTexture.buttonDefaultActive, LibraryTexture.buttonDefaultPressed, new RectOffset(2, 2, 2, 2), TextAnchor.MiddleCenter);
            buttonDefaultMiddleLeft = CreateButton(LibraryTexture.buttonDefault, LibraryTexture.buttonDefaultPressed, new RectOffset(2, 2, 2, 2), TextAnchor.MiddleLeft);
            buttonDefaultMiddleLeftActive = CreateButton(LibraryTexture.buttonDefaultActive, LibraryTexture.buttonDefaultPressed, new RectOffset(2, 2, 2, 2), TextAnchor.MiddleLeft);
            buttonSubMenu = CreateButton(LibraryTexture.gScale15_100, LibraryTexture.gScale40_100, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleCenter);
            buttonSubMenu2 = CreateButton(LibraryTexture.gScale70_100, LibraryTexture.gScale90_100, new RectOffset(0, 0, 0, 0), TextAnchor.UpperCenter);
            buttonSubMenuActive = CreateButton(LibraryTexture.gScale7_100, LibraryTexture.gScale7_100, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleCenter);

            ReassureCustomEditorData();
        }

        private static GUIStyle CreateButton(Texture2D texture, Texture2D textureActive, RectOffset margin, TextAnchor textAnchor)
        {
            GUIStyle button = new GUIStyle
            {
                normal = { background = texture },
                hover = { background = texture },
                active = { background = textureActive },
                border = new RectOffset(3, 3, 3, 3),
                padding = new RectOffset(6, 6, 3, 3),
                margin = margin,
                alignment = textAnchor,
            };

            button.normal.textColor = Color.white;
            button.focused.textColor = Color.white;
            button.active.textColor = Color.white;
            button.hover.textColor = Color.white;

            return button;
        }

        private static GUIStyle CreateScrollBar()
        {
            GUIStyle style = new GUIStyle(GUI.skin.verticalScrollbar);
            style.normal.background = LibraryTexture.gScale10_100;

            return style;
        }

        private static GUIStyle CreatePopUpFiled()
        {
            GUIStyle style = new GUIStyle()
            {
                normal = {
                    background = LibraryTexture.popUpField,
                    textColor = Color.white
                },
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                fixedHeight = 18f,
                padding = new RectOffset(6, 20, 3, 3),
                margin = new RectOffset(2, 2, 2, 2),
                border = new RectOffset(3, 16, 3, 3),
                clipping = TextClipping.Clip
            };

            return style;
        }

        private static GUIStyle CreateBackgroundStyle(Texture2D texture, float size)
        {
            GUIStyle style = new GUIStyle();
            style.normal.background = texture;
            if(size > 0) style.fixedHeight = size;

            return style;
        }

        private static GUIStyle CreateBoxStyle(Texture2D texture, float height, float width, RectOffset margin)
        {
            GUIStyle style = new GUIStyle();
            style.normal.background = texture;
            style.fixedHeight = height;
            style.fixedWidth = width;
            style.margin = margin;

            return style;
        }

        private static GUIStyle CreateTextStyle(Color color, RectOffset padding, RectOffset margin, int size, float elementWidth = 0, float elementHeight = 0, bool centerAlignment = false)
        {
            GUIStyle textStyle = new GUIStyle();

            if(elementWidth != 0)
                textStyle.fixedWidth = elementWidth;

            if (elementHeight != 0) textStyle.fixedHeight = elementHeight;
            else textStyle.fixedHeight = WindowBase.itemHeight;
            textStyle.richText = true;
            textStyle.padding = padding;
            textStyle.margin = margin;
            textStyle.normal.textColor = color;
            textStyle.fontSize = size;
            textStyle.alignment = TextAnchor.MiddleLeft;
            if(centerAlignment) textStyle.alignment = TextAnchor.MiddleCenter;

            return textStyle;
        }

        private static GUIStyle CreateTextField(float width, float height, RectOffset margin, Texture2D normalBackground, Texture2D activeBackground)
        {
            GUIStyle textField = new GUIStyle
            {
                normal = {
                    background = normalBackground,
                    textColor = Color.white,
                },
                focused = {
                    background = activeBackground,
                    textColor = Color.white
                },
                active = {
                    background = activeBackground,
                    textColor = Color.white
                },
                border = new RectOffset(4, 4, 4, 4),
                padding = new RectOffset(4, 4, 2, 2),
                margin = margin,
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                fixedHeight = height,
                fixedWidth = width,
                clipping = TextClipping.Clip,
                wordWrap = false
            };

            return textField;
        }

        private static GUIStyle CreateInfoBox(float width, float height, int fontSize)
        {
            GUIStyle textField = new GUIStyle
            {
                normal = {
                    background = LibraryTexture.infoBox,
                    textColor = Color.black,
                },
                focused = {
                    background = LibraryTexture.infoBox,
                    textColor = Color.black
                },
                active = {
                    background = LibraryTexture.infoBox,
                    textColor = Color.black
                },
                border = new RectOffset(4, 4, 4, 4),
                padding = new RectOffset(2, 2, 1, 1),
                alignment = TextAnchor.MiddleLeft,
                fontSize = fontSize,
                fixedHeight = height,
                fixedWidth = width,
                wordWrap = false
            };

            return textField;
        }

        private static GUIStyle CreateSceneViewText(float height, RectOffset padding)
        {
            GUIStyle textField = new GUIStyle
            {
                normal = {
                    background = LibraryTexture.inputField,
                    textColor = Color.white,
                },
                focused = {
                    background = LibraryTexture.inputFieldActive,
                    textColor = Color.white
                },
                active = {
                    background = LibraryTexture.inputFieldActive,
                    textColor = Color.white
                },
                border = new RectOffset(4, 4, 4, 4),
                padding = padding,
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fixedHeight = height,
            };

            return textField;
        }

        private static GUIStyle CreateInfoMsgIcon()
        {
            GUIStyle textStyle = new GUIStyle();
            textStyle.fixedWidth = 13;
            textStyle.fixedHeight = 13;
            textStyle.padding = new RectOffset(0, 0, 0, 0);
            textStyle.margin = new RectOffset(5, 3, 3, 0);
            textStyle.border = new RectOffset(0, 0, 0, 0);

            return textStyle;
        }

        internal static void ReassureCustomEditorData()
        {
            if (componentInfo == null)
                componentInfo = new GUIStyle(EditorStyles.wordWrappedLabel);
        }
    }
}
