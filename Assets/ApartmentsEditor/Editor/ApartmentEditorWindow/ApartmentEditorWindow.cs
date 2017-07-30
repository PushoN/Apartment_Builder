﻿using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Nox7atra.ApartmentEditor
{
    public sealed class ApartmentEditorWindow : EditorWindow
    {
        #region factory
        [MenuItem("Window/ApartmentBuilder")]
        public static void Create()
        {
            var window = GetWindow<ApartmentEditorWindow>("ApartmentBuilder");

            window._ConfigWindow = ApartmentConfigWindow.Create(window);
            window.Show();
        }
        #endregion

        #region nested types
        public enum EditorWindowState
        {
            Normal,
            RoomCreation
        }
        #endregion

        #region callbacks
        public event Action<EventType, Event> onKeyEvent;
        #endregion

        #region properties
        public Apartment CurrentApartment
        {
            get
            {
                return _ApartmentManager.CurrentApartment;
            }
        }
        #endregion
        #region attributes
        public  readonly Grid Grid;

        private readonly Toolbar _Toolbar;
        private ApartmentConfigWindow _ConfigWindow;

        private readonly ApartmentsManager _ApartmentManager;
        private readonly Dictionary<EditorWindowState, StateApartmentBuilder> _States;
        private Vector3? _LastMousePosition;
        #endregion

        #region public methods
        
        public void CreateRoomBegin()
        {
            ActivateState(EditorWindowState.RoomCreation);
        }
        public void CreateRoomEnd(Room room)
        {
            _ApartmentManager.CurrentApartment.Rooms.Add(room);
            ActivateState(EditorWindowState.Normal);
            Repaint();
        }
        #endregion

        #region keys
        void KeysEvents()
        {
            var curEvent = Event.current;
            switch (curEvent.type)
            {
                case EventType.MouseDrag:
                    if (Event.current.button == 1)
                        DragGrid();
                    break;
                case EventType.MouseDown:
                    if (Event.current.button == 1)
                        _LastMousePosition = null;
                    break;
            }

            if (onKeyEvent != null)
            {
                onKeyEvent(curEvent.type, curEvent);
            }
            else if (Event.current.rawType == EventType.MouseUp)
            {
                if (onKeyEvent != null)
                    onKeyEvent(Event.current.rawType, curEvent);
            }
        }
        void DragGrid()
        {
            var curMousePosition = Event.current.mousePosition;
            if (_LastMousePosition.HasValue)
            {
                var dv = GUIUtility.GUIToScreenPoint((Vector2)_LastMousePosition)
                         - GUIUtility.GUIToScreenPoint(curMousePosition);
                Grid.Move(dv);
                Repaint();
            }
            _LastMousePosition = curMousePosition;
        }
        #endregion

        #region service methods
        void ActivateState(EditorWindowState state)
        {
            foreach (var stateApartmentEditor in _States)
            {
                stateApartmentEditor.Value.SetActive(stateApartmentEditor.Key == state);
            }
        }
        #endregion

        #region engine methods
        void OnGUI()
        {
            KeysEvents();
            Grid.Draw();
            var apartment = _ApartmentManager.CurrentApartment;
            if (apartment != null)
            {
                apartment.Draw(Grid);
            }

            _Toolbar.Draw();
            foreach (var stateApartmentEditor in _States)
            {
                stateApartmentEditor.Value.Draw();
            }
           

        }
        void OnDestroy()
        {
            foreach (var stateApartmentEditor in _States.Values)
            {
                stateApartmentEditor.Destroy();
            }
        }
        #endregion

        #region constructors
        public ApartmentEditorWindow()
        {
            Grid = new Grid(this);
            _Toolbar = new Toolbar(this);

            _ApartmentManager = new ApartmentsManager();
            wantsMouseMove = true;
            _States = new Dictionary<EditorWindowState, StateApartmentBuilder>
            {
                {EditorWindowState.Normal,       new NormalState(this)},
                {EditorWindowState.RoomCreation, new CreatingRoomState(this)}
            };
            ActivateState(EditorWindowState.Normal);
        }
        #endregion
    }
}