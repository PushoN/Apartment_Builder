﻿using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Nox7atra.ApartmentEditor
{
    public class ApartmentConfigWindow : EditorWindow
    {
        #region factory
        public static ApartmentConfigWindow Create(ApartmentEditorWindow parent)
        {
            var window = GetWindow<ApartmentConfigWindow>("ApartmentConfig");
            window._Parent = parent;
            window.Show();
            return window;
        }
        #endregion

        #region attributes
        public static ApartmentDrawConfig Config;

        private ApartmentEditorWindow _Parent;
        #endregion
        #region engine methods
        void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            _Parent.CurrentApartment.Dimensions = EditorGUILayout.Vector2Field(
                "Dimensions", _Parent.CurrentApartment.Dimensions);

            Config.IsDrawPositions = GUILayout.Toggle(Config.IsDrawPositions, "Show positions");

            Config.IsDrawSizes = GUILayout.Toggle(Config.IsDrawSizes, "Show sizes");
            EditorGUILayout.EndVertical();
            _Parent.Repaint();
        }
        #endregion
        public ApartmentConfigWindow()
        {
            Config = new ApartmentDrawConfig();
        }
    }
    public class ApartmentDrawConfig
    {
        public bool IsDrawSizes;
        public bool IsDrawPositions;
        public ApartmentDrawConfig()
        {
            IsDrawSizes = true;
            IsDrawPositions = false;
        }
    }
}