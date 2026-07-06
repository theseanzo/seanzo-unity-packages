using System.Collections.Generic;
using UnityEngine;

namespace Seanzo.LevelDesign
{
    public static class GridMath
    {
        public static Vector3 CellMinLocal(Vector3Int cell, Vector3 cellSize)
        {
            return Vector3.Scale(cell, cellSize);
        }

        public static Vector3 CellCenterLocal(Vector3Int cell, Vector3 cellSize)
        {
            return Vector3.Scale(cell + new Vector3(0.5f, 0.5f, 0.5f), cellSize);
        }

        public static Vector3Int LocalToCell(Vector3 localPosition, Vector3 cellSize)
        {
            return new Vector3Int(
                Mathf.FloorToInt(localPosition.x / cellSize.x),
                Mathf.FloorToInt(localPosition.y / cellSize.y),
                Mathf.FloorToInt(localPosition.z / cellSize.z));
        }

        public static Vector3Int FaceNormal(CellFace face)
        {
            return face switch
            {
                CellFace.NegX => Vector3Int.left,
                CellFace.PosX => Vector3Int.right,
                CellFace.NegY => Vector3Int.down,
                CellFace.PosY => Vector3Int.up,
                CellFace.NegZ => new Vector3Int(0, 0, -1),
                _ => new Vector3Int(0, 0, 1)
            };
        }

        public static CellFace Opposite(CellFace face)
        {
            return face switch
            {
                CellFace.NegX => CellFace.PosX,
                CellFace.PosX => CellFace.NegX,
                CellFace.NegY => CellFace.PosY,
                CellFace.PosY => CellFace.NegY,
                CellFace.NegZ => CellFace.PosZ,
                _ => CellFace.NegZ
            };
        }

        public static bool TryFaceBetween(Vector3Int from, Vector3Int to, out CellFace face)
        {
            var delta = to - from;
            face = default;
            if (delta.sqrMagnitude != 1)
            {
                return false;
            }
            face = delta.x == -1 ? CellFace.NegX
                : delta.x == 1 ? CellFace.PosX
                : delta.y == -1 ? CellFace.NegY
                : delta.y == 1 ? CellFace.PosY
                : delta.z == -1 ? CellFace.NegZ
                : CellFace.PosZ;
            return true;
        }

        public static IEnumerable<Vector3Int> InPlaneNeighbors(Vector3Int cell, PlaneOrientation orientation)
        {
            var plane = new PaintPlane(orientation, 0);
            var (axisA, axisB) = plane.InPlaneAxes;
            for (var da = -1; da <= 1; da++)
            {
                for (var db = -1; db <= 1; db++)
                {
                    if (da == 0 && db == 0)
                    {
                        continue;
                    }
                    var neighbor = cell;
                    neighbor[axisA] += da;
                    neighbor[axisB] += db;
                    yield return neighbor;
                }
            }
        }

        public static IEnumerable<Vector3Int> FootprintCells(Vector3Int anchor, Vector3Int footprint)
        {
            for (var x = 0; x < footprint.x; x++)
            {
                for (var y = 0; y < footprint.y; y++)
                {
                    for (var z = 0; z < footprint.z; z++)
                    {
                        yield return anchor + new Vector3Int(x, y, z);
                    }
                }
            }
        }

        public static Vector3Int RotateFootprint(Vector3Int footprint, int rotationStep)
        {
            var step = ((rotationStep % 4) + 4) % 4;
            return step % 2 == 0
                ? footprint
                : new Vector3Int(footprint.z, footprint.y, footprint.x);
        }

        public static Vector3 FaceAnchorLocal(Vector3Int cell, CellFace face, Vector3 cellSize)
        {
            var min = CellMinLocal(cell, cellSize);
            var center = CellCenterLocal(cell, cellSize);
            return face switch
            {
                CellFace.NegX => new Vector3(min.x, min.y, center.z),
                CellFace.PosX => new Vector3(min.x + cellSize.x, min.y, center.z),
                CellFace.NegY => new Vector3(center.x, min.y, center.z),
                CellFace.PosY => new Vector3(center.x, min.y + cellSize.y, center.z),
                CellFace.NegZ => new Vector3(center.x, min.y, min.z),
                _ => new Vector3(center.x, min.y, min.z + cellSize.z)
            };
        }

        public static Vector3 AxisVector(AuthoredAxis axis)
        {
            return axis switch
            {
                AuthoredAxis.PosX => Vector3.right,
                AuthoredAxis.NegX => Vector3.left,
                AuthoredAxis.PosY => Vector3.up,
                AuthoredAxis.NegY => Vector3.down,
                AuthoredAxis.NegZ => Vector3.back,
                _ => Vector3.forward
            };
        }

        // Pre-rotation mapping a piece's authored basis onto the canonical one
        // (+Z facing, +Y up). An up collinear with the facing falls back to the
        // canonical up for that facing, so legacy data (both axes zero) resolves
        // to identity.
        public static Quaternion AuthoredBasis(AuthoredAxis facing, AuthoredAxis up)
        {
            var facingVec = AxisVector(facing);
            var upVec = AxisVector(up);
            if (Mathf.Abs(Vector3.Dot(facingVec, upVec)) > 0.5f)
            {
                upVec = facing is AuthoredAxis.PosY or AuthoredAxis.NegY ? Vector3.forward : Vector3.up;
            }
            return Quaternion.Inverse(Quaternion.LookRotation(facingVec, upVec));
        }

        public static Quaternion FaceRotation(CellFace face, int rotationStep)
        {
            var normal = (Vector3)FaceNormal(face);
            var up = face == CellFace.NegY || face == CellFace.PosY ? Vector3.forward : Vector3.up;
            var yaw = ((rotationStep % 4) + 4) % 4 * 90f;
            return Quaternion.LookRotation(normal, up) * Quaternion.Euler(0f, yaw, 0f);
        }

        public static void FaceCornersLocal(Vector3Int cell, CellFace face, Vector3 cellSize, Vector3[] corners)
        {
            var min = CellMinLocal(cell, cellSize);
            var max = min + cellSize;
            switch (face)
            {
                case CellFace.NegX:
                case CellFace.PosX:
                {
                    var x = face == CellFace.NegX ? min.x : max.x;
                    corners[0] = new Vector3(x, min.y, min.z);
                    corners[1] = new Vector3(x, max.y, min.z);
                    corners[2] = new Vector3(x, max.y, max.z);
                    corners[3] = new Vector3(x, min.y, max.z);
                    break;
                }
                case CellFace.NegY:
                case CellFace.PosY:
                {
                    var y = face == CellFace.NegY ? min.y : max.y;
                    corners[0] = new Vector3(min.x, y, min.z);
                    corners[1] = new Vector3(max.x, y, min.z);
                    corners[2] = new Vector3(max.x, y, max.z);
                    corners[3] = new Vector3(min.x, y, max.z);
                    break;
                }
                default:
                {
                    var z = face == CellFace.NegZ ? min.z : max.z;
                    corners[0] = new Vector3(min.x, min.y, z);
                    corners[1] = new Vector3(max.x, min.y, z);
                    corners[2] = new Vector3(max.x, max.y, z);
                    corners[3] = new Vector3(min.x, max.y, z);
                    break;
                }
            }
        }

        public static bool TryVerticalPlaneTarget(
            PaintPlane plane, Vector3Int planeCell, bool farSide, out Vector3Int cell, out CellFace face)
        {
            cell = planeCell;
            face = default;
            switch (plane.orientation)
            {
                case PlaneOrientation.XY:
                    face = farSide ? CellFace.PosZ : CellFace.NegZ;
                    if (farSide)
                    {
                        cell.z -= 1;
                    }
                    return true;
                case PlaneOrientation.YZ:
                    face = farSide ? CellFace.PosX : CellFace.NegX;
                    if (farSide)
                    {
                        cell.x -= 1;
                    }
                    return true;
                default:
                    return false;
            }
        }
    }
}
