using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Seanzo.LevelDesign.Editor
{
    // Re-resolve methods mutate grid slots; callers must have registered the grid for undo first.
    public static class RuleResolution
    {
        public static Func<int, int, bool> ContentOccupancy(GridLevel grid, Vector3Int anchor)
        {
            return (da, db) => grid.IsContentOccupied(anchor + new Vector3Int(da, 0, db));
        }

        public static Func<int, int, bool> FaceOccupancy(GridLevel grid, Vector3Int cell, CellFace face)
        {
            var (axisA, axisB) = FaceAxes(face);
            return (da, db) =>
            {
                var neighbor = cell;
                neighbor[axisA] += da;
                neighbor[axisB] += db;
                return grid.GetFace(neighbor, face) != null;
            };
        }

        // In-plane axes for a face run: vertical faces pair their tangent axis with Y;
        // horizontal faces run in XZ.
        public static (int a, int b) FaceAxes(CellFace face)
        {
            return face switch
            {
                CellFace.NegX or CellFace.PosX => (2, 1),
                CellFace.NegZ or CellFace.PosZ => (0, 1),
                _ => (0, 2)
            };
        }

        public static void ReResolveContentNeighbors(GridLevel grid, IEnumerable<Vector3Int> changedCells)
        {
            var changed = new HashSet<Vector3Int>(changedCells);
            var candidates = new HashSet<Vector3Int>();
            foreach (var cell in changed)
            {
                foreach (var neighbor in GridMath.InPlaneNeighbors(cell, PlaneOrientation.XZ))
                {
                    if (!changed.Contains(neighbor))
                    {
                        candidates.Add(neighbor);
                    }
                }
            }
            foreach (var cell in candidates)
            {
                ReResolveContentCell(grid, cell);
            }
        }

        // Batch form for fills: one re-resolve per cell in the painted region and
        // its border, instead of one per neighbor per paint.
        public static void ReResolveContentRegion(GridLevel grid, IEnumerable<Vector3Int> changedCells)
        {
            var cells = new HashSet<Vector3Int>(changedCells);
            foreach (var cell in new List<Vector3Int>(cells))
            {
                foreach (var neighbor in GridMath.InPlaneNeighbors(cell, PlaneOrientation.XZ))
                {
                    cells.Add(neighbor);
                }
            }
            foreach (var cell in cells)
            {
                ReResolveContentCell(grid, cell);
            }
        }

        public static void ReResolveFaceRegion(GridLevel grid, IEnumerable<Vector3Int> changedCells, CellFace face)
        {
            var (axisA, axisB) = FaceAxes(face);
            var cells = new HashSet<Vector3Int>(changedCells);
            foreach (var cell in new List<Vector3Int>(cells))
            {
                for (var da = -1; da <= 1; da++)
                {
                    for (var db = -1; db <= 1; db++)
                    {
                        var neighbor = cell;
                        neighbor[axisA] += da;
                        neighbor[axisB] += db;
                        cells.Add(neighbor);
                    }
                }
            }
            foreach (var cell in cells)
            {
                ReResolveFaceCell(grid, cell, face);
            }
        }

        public static void ReResolveFaceNeighbors(GridLevel grid, Vector3Int cell, CellFace face)
        {
            var (axisA, axisB) = FaceAxes(face);
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
                    ReResolveFaceCell(grid, neighbor, face);
                }
            }
        }

        private static void ReResolveContentCell(GridLevel grid, Vector3Int cell)
        {
            if (!grid.TryGetContent(cell, out var anchor, out var slot) || anchor != cell || slot.ruleTile == null)
            {
                return;
            }
            RuleMatcher.Resolve(slot.ruleTile, ContentOccupancy(grid, anchor), out var prefab, out var rotation);
            if (prefab == null)
            {
                // Mirror the fresh-paint fallback so re-resolution and painting
                // agree when no rule matches and the tile has no default.
                prefab = slot.sourcePrefab;
            }
            if (prefab == null || (prefab == slot.prefab && rotation == slot.rotationStep))
            {
                return;
            }
            if (slot.instance != null)
            {
                Undo.DestroyObjectImmediate(slot.instance);
            }
            slot.prefab = prefab;
            slot.rotationStep = rotation;
            slot.instance = GridPaintOperations.PlaceContentInstance(
                grid, prefab, anchor, slot.footprint, rotation, "Re-resolve Rule Tile");
            LayerVisibilityUtility.ApplyToInstance(grid, slot.layerId, slot.instance);
        }

        private static void ReResolveFaceCell(GridLevel grid, Vector3Int cell, CellFace face)
        {
            var slot = grid.GetFace(cell, face);
            if (slot == null || slot.ruleTile == null)
            {
                return;
            }
            RuleMatcher.Resolve(slot.ruleTile, FaceOccupancy(grid, cell, face), out var prefab, out var rotation);
            if (prefab == null)
            {
                prefab = slot.sourcePrefab;
            }
            if (prefab == null || (prefab == slot.prefab && rotation == slot.rotationStep))
            {
                return;
            }
            if (slot.instance != null)
            {
                Undo.DestroyObjectImmediate(slot.instance);
            }
            slot.prefab = prefab;
            slot.rotationStep = rotation;
            slot.instance = GridPaintOperations.PlaceFaceInstance(
                grid, prefab, cell, face, rotation, slot.authoredFacing, slot.authoredUp,
                "Re-resolve Rule Tile");
            LayerVisibilityUtility.ApplyToInstance(grid, slot.layerId, slot.instance);
        }
    }
}
