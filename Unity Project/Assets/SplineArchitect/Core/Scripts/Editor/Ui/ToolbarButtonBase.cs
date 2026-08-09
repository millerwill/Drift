// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: ToolbarToggleBase.cs
//
// Author: Mikael Danielsson
// Date Created: 05-08-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace SplineArchitect.Ui
{
    public abstract class ToolbarButtonBase : EditorToolbarButton
    {
        public static List<ToolbarButtonBase> instances = new List<ToolbarButtonBase>();
        protected bool pointerHovering { get; private set; }

        public ToolbarButtonBase()
        {
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<PointerEnterEvent>(OnPointerEntered);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
            clicked += ToggleMenu;
        }


        private void OnPointerEntered(PointerEnterEvent evt)
        {
            pointerHovering = true;
            OnPointerLeaveEnter();
        }

        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            pointerHovering = false;
            OnPointerLeaveEnter();
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            if(!instances.Contains(this))
                instances.Add(this);
        }

        private void OnDetach(DetachFromPanelEvent evt)
        {
            instances.Remove(this);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            OnGeometryChangedExtended(parent.resolvedStyle.flexDirection == FlexDirection.Row);
        }

        protected virtual void OnGeometryChangedExtended(bool isHorizontal)
        {
            //The icon
            Image img = this.Q<UnityEngine.UIElements.Image>();

            if (img != null)
            {
                img.style.width = 14;
                img.style.height = 14;
                img.scaleMode = ScaleMode.StretchToFill;
            }
        }

        protected virtual void OnPointerLeaveEnter() {}

        protected abstract void ToggleMenu();
    }
}
