﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Foxsys.ApartmentEditor
{
    [Serializable]
    public class Room : ScriptableObject
    {
        public const float SNAPING_RAD = 10f;
        //factory
        public static Room Create(Apartment parent)
        {
            Room room =  CreateInstance<Room>();
            AssetDatabase.AddObjectToAsset(room, ApartmentsManager.Instance.CurrentApartment);
            room._ParentApartment = parent;
            room.ContourColor = new Color(Random.Range(0.5f, 1), Random.Range(0.5f, 1), Random.Range(0.5f, 1));
            EditorUtility.SetDirty(ApartmentsManager.Instance.CurrentApartment);
            room._Walls = new List<Wall>();
            AssetDatabase.SaveAssets();
            return room;
        }
        //data
        public Color ContourColor;

        public float WallThickness;

        [SerializeField]
        private Apartment _ParentApartment;

        [SerializeField]
        private List<Wall> _Walls;

        [SerializeField]
        public List<WallObject> _Objects;
        //properties
        public List<Wall> Walls
        {
            get
            {
                
                return _Walls;
            }
        }

        public Apartment ParentApartment
        {
            get { return _ParentApartment; }
        }
        public float Square
        {
            get
            {
                float result = 0;
                int pointsCount = _Walls.Count;
                for (int i = 0; i < pointsCount; i++)
                {
                    Wall wall = _Walls[i];
                    result +=
                        0.5f * (wall.Begin.x * wall.End.y - wall.Begin.y * wall.End.x);
                }
                return Mathf.Abs(result);
            }
        }

        public Vector2 Centroid
        {
            get
            {
                Vector2 centroid = new Vector2();
                float signedArea = 0;
                for(int i = 0; i < _Walls.Count; i++)
                {
                    Wall wall = _Walls[i];
                    float x0 = wall.Begin.x;
                    float y0 = wall.Begin.y;
                    float x1 = wall.End.x;
                    float y1 = wall.End.y;
                    float a = x0 * y1 - x1 * y0;
                    signedArea += a;
                    centroid.x += (x0 + x1) * a;
                    centroid.y += (y0 + y1) * a;
                }
                return centroid / (3 * signedArea);
            }
        }


        public void Draw(Grid grid)
        {
            foreach (Wall wall in _Walls)
            {
                var p1 = wall.Begin;
                var p2 = wall.End;

                wall.Draw(grid, ContourColor);
                Handles.color = Color.white;
                float rad = SNAPING_RAD / grid.Zoom;
                Handles.DrawWireDisc(grid.GridToGUI(p1), Vector3.back, rad);
                if (ApartmentConfig.Current.IsDrawSizes)
                {
                    Handles.color = Color.white;
                    Handles.Label(grid.GridToGUI((p1 + p2) / 2), 
                        Vector2.Distance(
                            p1,
                            p2).ToString());
                }
                if (ApartmentConfig.Current.IsDrawPositions)
                {
                    Handles.Label(grid.GridToGUI(p1) + new Vector2(SNAPING_RAD , SNAPING_RAD), p1.RoundCoordsToInt().ToString());
                }
            }
            if (WallThickness > 0)
            {
                var contour = GetContourWithThickness();
                for (int i = 0, count = contour.Count; i < contour.Count; i++)
                {
                    Handles.color = ContourColor / 1.5f;
                    Vector2 p1 = grid.GridToGUI(contour[i]), p2 = grid.GridToGUI(contour[(i + 1) % count]);
                    Handles.DrawLine(grid.GridToGUI(_Walls[i].End), p1);
                    Handles.DrawLine(p1, p2);
                }
            }
            if (ApartmentConfig.Current.IsDrawSquare)
            {
                Handles.Label(grid.GridToGUI(Centroid), Square.ToString());
            }
        }

        public bool Add(Vector2 point)
        {
            var wallsCount = _Walls.Count;
            if(wallsCount > 0)
            {
                _Walls[wallsCount - 1].End = point;
                if (IsLastPoint(point))
                {
                    _Walls[wallsCount - 1].End = _Walls[0].Begin;
                    
                    return false;
                }
            }
            Undo.RecordObject(this, "Add Room point");
            _Walls.Add(new Wall(point));
            return true;
        }
        public void Move(Vector2 dv)
        {
            foreach (Wall wall in _Walls)
            {
                wall.Move(dv);
            }
        }
        public void MoveVert(int index, Vector2 dv)
        {
            _Walls[index].Begin   += dv;
            if (index > 0)
                _Walls[index - 1].End += dv;
            else
                _Walls[_Walls.Count - 1].End += dv;
        }

        public void RemoveVert(int index)
        {
            if (index > 0)
            {
                _Walls[index - 1].End = _Walls[index].End;
            }
            else
            {
                _Walls[_Walls.Count - 1].End = _Walls[index].End;
            }
            _Walls.RemoveAt(index);
        }
      
        public void RoundContourPoints()
        {
            foreach (var wall in _Walls)
            {
                wall.Begin = wall.Begin.RoundCoordsToInt();
                wall.End = wall.End.RoundCoordsToInt();
            }
        }

        public Vector2 GetVertPosition(int index)
        {
            return _Walls[index].Begin;
        }

        public int GetContourVertIndex(Vector2 point)
        {
            for(int i = 0; i < _Walls.Count; i++)
            {
                if(Vector2.Distance(point, _Walls[i].Begin) < SNAPING_RAD)
                {
                    return i;
                }
            }
            return -1;
        }

        public List<Vector2> GetContour()
        {
            return _Walls.Select(x => x.Begin).ToList();
        }

        public List<Vector2> GetContourWithThickness()
        {
            List<Vector2> contour = new List<Vector2>(_Walls.Count);
            for (int i = 0, count = _Walls.Count; i < count; i++)
            {
                Wall wall1 = _Walls[i], wall2 = _Walls[(i + 1) % count];
                Vector2 p1 = wall1.Begin - wall1.Normal * WallThickness,
                    p2 = wall1.End - wall1.Normal * WallThickness,
                    p3 = wall2.Begin - wall2.Normal * WallThickness,
                    p4 = wall2.End - wall2.Normal * WallThickness;

                var intersection = MathUtils.LinesInterseciton(p1, p2, p3, p4);
                if (intersection.HasValue)
                    contour.Add(intersection.Value);
            }
            return contour;
        }
        public void MakeClockwiseOrientation()
        {
            var contour = GetContour();
            if (MathUtils.IsContourClockwise(contour))
            {
                _Walls.Reverse();
                foreach (var wall in _Walls)
                {
                    wall.Reverse();
                }
            }
        }

        public bool IsVertInsideRect(int vertNum, Rect rect)
        {
            return rect.Contains(_Walls[vertNum].Begin);
        }

        public bool IsInsideRect(Rect rect)
        {
            return _Walls.All(wall => rect.Contains(wall.Begin));
        }

        public bool IsLastPoint(Vector2 point)
        {
            return Vector2.Distance(point, _Walls[0].Begin) < SNAPING_RAD;
        }
    
        public enum Type
        {
            Kitchen,
            Bathroom,
            Toilet,
            BathroomAndToilet,
            None
        }
    }
    [Serializable]
    public class Wall
    {
        [SerializeField]
        public Vector2 Begin;
        [SerializeField]
        public Vector2 End;

        public void Reverse()
        {
            var tmp = Begin;
            Begin = End;
            End = tmp;

        }
        public Vector2 Center
        {
            get { return (End + Begin) / 2;  }
        }
        public Vector2 Normal { get { return new Vector2(Begin.y - End.y, End.x - Begin.x).normalized;} }
        public Vector2 Tangent { get { return new Vector2(Begin.x - End.x , Begin.y - End.y).normalized; } }
        public float Length { get { return Vector2.Distance(Begin, End); } }
        public float Rotation
        {
            get { return -Vector2.SignedAngle(Vector2.right, End - Begin); }
        }
        public void Move(Vector2 dv)
        {
            Begin += dv;
            End += dv;
        }
        public void Draw(Grid grid, Color color)
        {
            Handles.color = color;
            Handles.DrawLine(grid.GridToGUI(Begin), grid.GridToGUI(End));
        }
        public Wall()
        {

        }
        public Wall(Vector2 point)
        {
            Begin = point;
            End   = point;
        }
    }
    [Serializable]
    public abstract class WallObject
    {
        [SerializeField]
        public float WallPosition;  //from 0 to 1
        [SerializeField]
        public float Width;
        [SerializeField]
        public float Height;

        public void Reverse()
        {
            WallPosition = 1 - WallPosition;
        }
    }
    [Serializable]
    public class Door : WallObject
    {

    }
    [Serializable]
    public class Window : WallObject
    {
        [SerializeField]
        public float DistanceFromFloor;
    }
}