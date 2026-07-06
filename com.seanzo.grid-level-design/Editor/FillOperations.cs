using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Seanzo.LevelDesign.Editor
{
    public static class FillOperations
    {
        public const int UnboundedFloodCap = 4096;

        public static int BoxFill(
            GridLevel grid, KitPaletteEntry entry, PaintPlane plane,
            Vector3Int cornerA, Vector3Int cornerB, int rotationStep, int layerId, bool farSide)
        {
            if (grid == null || entry?.prefab == null)
            {
                return 0;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Box Fill");
            var group = Undo.GetCurrentGroup();
            var count = 0;

            if (plane.IsHorizontal)
            {
                if (entry.allowedSlot != SlotKind.Face)
                {
                    var footprint = GridMath.RotateFootprint(entry.Footprint, rotationStep);
                    var painted = new List<Vector3Int>();
                    foreach (var anchor in RectStrideAnchors(plane, cornerA, cornerB, footprint))
                    {
                        if (grid.CanPlaceContent(anchor, footprint)
                            && GridPaintOperations.Paint(grid, entry, anchor, rotationStep, layerId, reResolveNeighbors: false))
                        {
                            painted.AddRange(GridMath.FootprintCells(anchor, footprint));
                            count++;
                        }
                    }
                    RuleResolution.ReResolveContentRegion(grid, painted);
                }
            }
            else if (entry.allowedSlot != SlotKind.Content)
            {
                var painted = new List<Vector3Int>();
                CellFace paintedFace = default;
                foreach (var planeCell in RectCells(plane, cornerA, cornerB))
                {
                    if (GridMath.TryVerticalPlaneTarget(plane, planeCell, farSide, out var cell, out var face)
                        && grid.GetFace(cell, face) == null
                        && GridPaintOperations.PaintFace(grid, entry, cell, face, rotationStep, layerId, reResolveNeighbors: false))
                    {
                        painted.Add(cell);
                        paintedFace = face;
                        count++;
                    }
                }
                if (painted.Count > 0)
                {
                    RuleResolution.ReResolveFaceRegion(grid, painted, paintedFace);
                }
            }

            Undo.CollapseUndoOperations(group);
            return count;
        }

        public static int BoxErase(GridLevel grid, PaintPlane plane, Vector3Int cornerA, Vector3Int cornerB, bool farSide)
        {
            if (grid == null)
            {
                return 0;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Box Erase");
            var group = Undo.GetCurrentGroup();
            var count = 0;

            var erased = new List<Vector3Int>();
            CellFace erasedFace = default;
            foreach (var planeCell in RectCells(plane, cornerA, cornerB))
            {
                if (plane.IsHorizontal)
                {
                    var hadPiece = grid.TryGetContent(planeCell, out var pieceAnchor, out var pieceSlot);
                    if (GridPaintOperations.Erase(grid, planeCell, reResolveNeighbors: false))
                    {
                        if (hadPiece)
                        {
                            erased.AddRange(GridMath.FootprintCells(pieceAnchor, pieceSlot.footprint));
                        }
                        count++;
                    }
                }
                else if (GridMath.TryVerticalPlaneTarget(plane, planeCell, farSide, out var cell, out var face)
                    && GridPaintOperations.EraseFace(grid, cell, face, reResolveNeighbors: false))
                {
                    erased.Add(cell);
                    erasedFace = face;
                    count++;
                }
            }
            if (erased.Count > 0)
            {
                if (plane.IsHorizontal)
                {
                    RuleResolution.ReResolveContentRegion(grid, erased);
                }
                else
                {
                    RuleResolution.ReResolveFaceRegion(grid, erased, erasedFace);
                }
            }

            Undo.CollapseUndoOperations(group);
            return count;
        }

        public static int FloodFill(
            GridLevel grid, KitPaletteEntry entry, PaintPlane plane, Vector3Int start,
            int rotationStep, int layerId, bool farSide, out bool truncated, int cap = UnboundedFloodCap)
        {
            truncated = false;
            if (grid == null || entry?.prefab == null)
            {
                return 0;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Flood Fill");
            var group = Undo.GetCurrentGroup();
            var count = 0;

            if (plane.IsHorizontal)
            {
                if (entry.allowedSlot != SlotKind.Face)
                {
                    var region = ContentFloodRegion(grid, plane, start, cap, out truncated);
                    var changed = new List<Vector3Int>();
                    var replacing = grid.IsContentOccupied(start);
                    if (replacing)
                    {
                        foreach (var anchor in RegionPieceAnchors(grid, region))
                        {
                            if (grid.TryGetContent(anchor, out _, out var pieceSlot))
                            {
                                changed.AddRange(GridMath.FootprintCells(anchor, pieceSlot.footprint));
                            }
                            GridPaintOperations.Erase(grid, anchor, reResolveNeighbors: false);
                        }
                    }
                    var footprint = GridMath.RotateFootprint(entry.Footprint, rotationStep);
                    foreach (var anchor in StrideAnchors(plane, region, footprint))
                    {
                        if (grid.CanPlaceContent(anchor, footprint)
                            && GridPaintOperations.Paint(grid, entry, anchor, rotationStep, layerId, reResolveNeighbors: false))
                        {
                            changed.AddRange(GridMath.FootprintCells(anchor, footprint));
                            count++;
                        }
                    }
                    RuleResolution.ReResolveContentRegion(grid, changed);
                }
            }
            else if (entry.allowedSlot != SlotKind.Content)
            {
                var region = FaceFloodRegion(grid, plane, start, farSide, cap, out truncated);
                var changed = new List<Vector3Int>();
                CellFace changedFace = default;
                foreach (var planeCell in region)
                {
                    if (!GridMath.TryVerticalPlaneTarget(plane, planeCell, farSide, out var cell, out var face))
                    {
                        continue;
                    }
                    if (grid.GetFace(cell, face) != null)
                    {
                        GridPaintOperations.EraseFace(grid, cell, face, reResolveNeighbors: false);
                    }
                    if (GridPaintOperations.PaintFace(grid, entry, cell, face, rotationStep, layerId, reResolveNeighbors: false))
                    {
                        changed.Add(cell);
                        changedFace = face;
                        count++;
                    }
                }
                if (changed.Count > 0)
                {
                    RuleResolution.ReResolveFaceRegion(grid, changed, changedFace);
                }
            }

            Undo.CollapseUndoOperations(group);
            return count;
        }

        public static IEnumerable<Vector3Int> RectCells(PaintPlane plane, Vector3Int cornerA, Vector3Int cornerB)
        {
            var (a0, b0) = plane.InPlaneCoords(cornerA);
            var (a1, b1) = plane.InPlaneCoords(cornerB);
            var minA = Mathf.Min(a0, a1);
            var maxA = Mathf.Max(a0, a1);
            var minB = Mathf.Min(b0, b1);
            var maxB = Mathf.Max(b0, b1);
            for (var a = minA; a <= maxA; a++)
            {
                for (var b = minB; b <= maxB; b++)
                {
                    yield return plane.CellAt(a, b);
                }
            }
        }

        public static List<Vector3Int> RectStrideAnchors(PaintPlane plane, Vector3Int cornerA, Vector3Int cornerB, Vector3Int footprint)
        {
            return StrideAnchors(plane, RectCells(plane, cornerA, cornerB).ToList(), footprint);
        }

        public static List<Vector3Int> StrideAnchors(PaintPlane plane, List<Vector3Int> region, Vector3Int footprint)
        {
            var result = new List<Vector3Int>();
            if (region.Count == 0)
            {
                return result;
            }

            var set = new HashSet<Vector3Int>(region);
            var (axisA, axisB) = plane.InPlaneAxes;
            var strideA = Mathf.Max(1, footprint[axisA]);
            var strideB = Mathf.Max(1, footprint[axisB]);

            var minA = region.Min(c => c[axisA]);
            var minB = region.Min(c => c[axisB]);

            foreach (var cell in region.OrderBy(c => c[axisA]).ThenBy(c => c[axisB]))
            {
                if ((cell[axisA] - minA) % strideA != 0 || (cell[axisB] - minB) % strideB != 0)
                {
                    continue;
                }
                var fits = true;
                for (var da = 0; da < strideA && fits; da++)
                {
                    for (var db = 0; db < strideB && fits; db++)
                    {
                        var covered = cell;
                        covered[axisA] += da;
                        covered[axisB] += db;
                        fits = set.Contains(covered);
                    }
                }
                if (fits)
                {
                    result.Add(cell);
                }
            }
            return result;
        }

        public static List<Vector3Int> ContentFloodRegion(
            GridLevel grid, PaintPlane plane, Vector3Int start, int cap, out bool truncated)
        {
            var key = grid.TryGetContent(start, out _, out var startSlot) ? startSlot.prefab : null;
            bool Matches(Vector3Int cell)
            {
                return key == null
                    ? !grid.IsContentOccupied(cell)
                    : grid.TryGetContent(cell, out _, out var slot) && slot.prefab == key;
            }
            return FloodRegion(grid, plane, start, cap, Matches, out truncated);
        }

        public static List<Vector3Int> FaceFloodRegion(
            GridLevel grid, PaintPlane plane, Vector3Int start, bool farSide, int cap, out bool truncated)
        {
            GameObject key = null;
            if (GridMath.TryVerticalPlaneTarget(plane, start, farSide, out var startCell, out var startFace))
            {
                key = grid.GetFace(startCell, startFace)?.prefab;
            }
            bool Matches(Vector3Int planeCell)
            {
                if (!GridMath.TryVerticalPlaneTarget(plane, planeCell, farSide, out var cell, out var face)
                    || !grid.InBounds(cell))
                {
                    return false;
                }
                var slot = grid.GetFace(cell, face);
                return key == null ? slot == null : slot?.prefab == key;
            }
            return FloodRegion(grid, plane, start, cap, Matches, out truncated);
        }

        private static List<Vector3Int> FloodRegion(
            GridLevel grid, PaintPlane plane, Vector3Int start, int cap,
            System.Func<Vector3Int, bool> matches, out bool truncated)
        {
            truncated = false;
            var region = new List<Vector3Int>();
            if (!grid.InBounds(start) || !matches(start))
            {
                return region;
            }

            var (axisA, axisB) = plane.InPlaneAxes;
            var visited = new HashSet<Vector3Int> { start };
            var queue = new Queue<Vector3Int>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                if (region.Count >= cap)
                {
                    truncated = true;
                    break;
                }
                var cell = queue.Dequeue();
                region.Add(cell);

                for (var i = 0; i < 4; i++)
                {
                    var neighbor = cell;
                    var axis = i < 2 ? axisA : axisB;
                    neighbor[axis] += i % 2 == 0 ? 1 : -1;
                    if (visited.Contains(neighbor) || !grid.InBounds(neighbor) || !matches(neighbor))
                    {
                        continue;
                    }
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
            return region;
        }

        private static List<Vector3Int> RegionPieceAnchors(GridLevel grid, List<Vector3Int> region)
        {
            var anchors = new List<Vector3Int>();
            foreach (var cell in region)
            {
                if (grid.TryGetContentAnchor(cell, out var anchor) && !anchors.Contains(anchor))
                {
                    anchors.Add(anchor);
                }
            }
            return anchors;
        }
    }
}
