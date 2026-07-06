using System;
using UnityEngine;

namespace Seanzo.LevelDesign
{
    [Serializable]
    public struct PaintPlane : IEquatable<PaintPlane>
    {
        public PlaneOrientation orientation;
        public int index;

        public PaintPlane(PlaneOrientation orientation, int index)
        {
            this.orientation = orientation;
            this.index = index;
        }

        public int NormalAxis => orientation switch
        {
            PlaneOrientation.XZ => 1,
            PlaneOrientation.XY => 2,
            _ => 0
        };

        public bool IsHorizontal => orientation == PlaneOrientation.XZ;

        public (int a, int b) InPlaneAxes => orientation switch
        {
            PlaneOrientation.XZ => (0, 2),
            PlaneOrientation.XY => (0, 1),
            _ => (2, 1)
        };

        public Vector3Int CellAt(int a, int b)
        {
            var (axisA, axisB) = InPlaneAxes;
            var cell = Vector3Int.zero;
            cell[axisA] = a;
            cell[axisB] = b;
            cell[NormalAxis] = index;
            return cell;
        }

        public (int a, int b) InPlaneCoords(Vector3Int cell)
        {
            var (axisA, axisB) = InPlaneAxes;
            return (cell[axisA], cell[axisB]);
        }

        public bool Contains(Vector3Int cell)
        {
            return cell[NormalAxis] == index;
        }

        public bool Equals(PaintPlane other)
        {
            return orientation == other.orientation && index == other.index;
        }

        public override bool Equals(object obj)
        {
            return obj is PaintPlane other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)orientation, index);
        }

        public override string ToString()
        {
            return $"{orientation}[{index}]";
        }
    }
}
