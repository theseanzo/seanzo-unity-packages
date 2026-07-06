using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Seanzo.LevelDesign.Editor
{
    public static class GridPaintOperations
    {
        public static int PruneMissing(GridLevel grid)
        {
            if (grid == null || grid.CountMissingInstances() == 0)
            {
                return 0;
            }
            Undo.RegisterCompleteObjectUndo(grid, "Prune Missing Cells");
            var removed = grid.PruneMissingInstances();
            EditorUtility.SetDirty(grid);
            return removed;
        }

        public static bool CanPaint(GridLevel grid, KitPaletteEntry entry, Vector3Int anchor, int rotationStep)
        {
            if (grid == null || entry == null || !HasPaintablePayload(entry))
            {
                return false;
            }
            var footprint = GridMath.RotateFootprint(entry.Footprint, rotationStep);
            return grid.CanPlaceContent(anchor, footprint) || CanReplace(grid, anchor, footprint);
        }

        public static bool Paint(
            GridLevel grid, KitPaletteEntry entry, Vector3Int anchor, int rotationStep, int layerId,
            bool reResolveNeighbors = true)
        {
            if (!CanPaint(grid, entry, anchor, rotationStep))
            {
                return false;
            }
            var footprint = GridMath.RotateFootprint(entry.Footprint, rotationStep);

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Paint Cell");
            var group = Undo.GetCurrentGroup();
            Undo.RegisterCompleteObjectUndo(grid, "Paint Cell");

            var changed = new HashSet<Vector3Int>();
            foreach (var pieceAnchor in OverlappingAnchors(grid, anchor, footprint))
            {
                if (grid.ClearContent(pieceAnchor, out _, out var removed))
                {
                    changed.UnionWith(GridMath.FootprintCells(pieceAnchor, removed.footprint));
                    if (removed.instance != null)
                    {
                        Undo.DestroyObjectImmediate(removed.instance);
                    }
                }
            }

            var prefab = entry.prefab;
            var rotation = rotationStep;
            if (entry.ruleTile != null)
            {
                RuleMatcher.Resolve(entry.ruleTile, RuleResolution.ContentOccupancy(grid, anchor), out prefab, out rotation);
                if (prefab == null)
                {
                    prefab = entry.prefab;
                }
            }

            var instance = PlaceContentInstance(grid, prefab, anchor, footprint, rotation, "Paint Cell");

            grid.SetContent(anchor, new ContentSlot
            {
                prefab = prefab,
                rotationStep = rotation,
                layerId = layerId,
                instance = instance,
                footprint = footprint,
                ruleTile = entry.ruleTile,
                sourcePrefab = entry.prefab
            });
            changed.UnionWith(GridMath.FootprintCells(anchor, footprint));

            LayerVisibilityUtility.ApplyToInstance(grid, layerId, instance);
            if (reResolveNeighbors)
            {
                RuleResolution.ReResolveContentNeighbors(grid, changed);
            }
            EditorUtility.SetDirty(grid);
            Undo.CollapseUndoOperations(group);
            return true;
        }

        public static bool CanPaintFace(GridLevel grid, KitPaletteEntry entry, Vector3Int cell)
        {
            return grid != null && entry != null && HasPaintablePayload(entry) && grid.InBounds(cell);
        }

        public static bool PaintFace(
            GridLevel grid, KitPaletteEntry entry, Vector3Int cell, CellFace face, int rotationStep, int layerId,
            bool reResolveNeighbors = true)
        {
            if (!CanPaintFace(grid, entry, cell))
            {
                return false;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Paint Face");
            var group = Undo.GetCurrentGroup();
            Undo.RegisterCompleteObjectUndo(grid, "Paint Face");

            if (grid.ClearFace(cell, face, out var existing) && existing.instance != null)
            {
                Undo.DestroyObjectImmediate(existing.instance);
            }

            var prefab = entry.prefab;
            var rotation = rotationStep;
            if (entry.ruleTile != null)
            {
                RuleMatcher.Resolve(entry.ruleTile, RuleResolution.FaceOccupancy(grid, cell, face), out prefab, out rotation);
                if (prefab == null)
                {
                    prefab = entry.prefab;
                }
            }

            var instance = PlaceFaceInstance(
                grid, prefab, cell, face, rotation, entry.authoredFacing, entry.authoredUp, "Paint Face");

            grid.SetFace(cell, face, new CellSlot
            {
                prefab = prefab,
                rotationStep = rotation,
                layerId = layerId,
                instance = instance,
                ruleTile = entry.ruleTile,
                sourcePrefab = entry.prefab,
                authoredFacing = entry.authoredFacing,
                authoredUp = entry.authoredUp
            });

            LayerVisibilityUtility.ApplyToInstance(grid, layerId, instance);
            if (reResolveNeighbors)
            {
                RuleResolution.ReResolveFaceNeighbors(grid, cell, face);
            }
            EditorUtility.SetDirty(grid);
            Undo.CollapseUndoOperations(group);
            return true;
        }

        public static bool EraseFace(GridLevel grid, Vector3Int cell, CellFace face, bool reResolveNeighbors = true)
        {
            if (grid == null || grid.GetFace(cell, face) == null)
            {
                return false;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Erase Face");
            var group = Undo.GetCurrentGroup();
            Undo.RegisterCompleteObjectUndo(grid, "Erase Face");

            if (!grid.ClearFace(cell, face, out var removed))
            {
                return false;
            }
            if (removed.instance != null)
            {
                Undo.DestroyObjectImmediate(removed.instance);
            }
            if (reResolveNeighbors)
            {
                RuleResolution.ReResolveFaceNeighbors(grid, cell, face);
            }
            EditorUtility.SetDirty(grid);
            Undo.CollapseUndoOperations(group);
            return true;
        }

        public static bool TryPickFace(GridLevel grid, Vector3Int cell, CellFace face, out GameObject prefab, out int rotationStep)
        {
            prefab = null;
            rotationStep = 0;
            var slot = grid != null ? grid.GetFace(cell, face) : null;
            if (slot == null)
            {
                return false;
            }
            prefab = slot.prefab;
            rotationStep = slot.rotationStep;
            return true;
        }

        public static bool Erase(GridLevel grid, Vector3Int cell, bool reResolveNeighbors = true)
        {
            if (grid == null || !grid.IsContentOccupied(cell))
            {
                return false;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Erase Cell");
            var group = Undo.GetCurrentGroup();
            Undo.RegisterCompleteObjectUndo(grid, "Erase Cell");

            if (!grid.ClearContent(cell, out var anchor, out var removed))
            {
                return false;
            }
            if (removed.instance != null)
            {
                Undo.DestroyObjectImmediate(removed.instance);
            }
            if (reResolveNeighbors)
            {
                RuleResolution.ReResolveContentNeighbors(grid, GridMath.FootprintCells(anchor, removed.footprint));
            }
            EditorUtility.SetDirty(grid);
            Undo.CollapseUndoOperations(group);
            return true;
        }

        public static bool TryPick(GridLevel grid, Vector3Int cell, out GameObject prefab, out int rotationStep)
        {
            prefab = null;
            rotationStep = 0;
            if (grid == null || !grid.TryGetContent(cell, out _, out var slot))
            {
                return false;
            }
            prefab = slot.prefab;
            rotationStep = slot.rotationStep;
            return true;
        }

        internal static GameObject PlaceContentInstance(
            GridLevel grid, GameObject prefab, Vector3Int anchor, Vector3Int footprint, int rotationStep, string undoName)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, grid.transform);
            Undo.RegisterCreatedObjectUndo(instance, undoName);
            var cellSize = grid.CellSize;
            var min = GridMath.CellMinLocal(anchor, cellSize);
            var size = Vector3.Scale(footprint, cellSize);
            instance.transform.localPosition = min + new Vector3(size.x * 0.5f, 0f, size.z * 0.5f);
            instance.transform.localRotation = Quaternion.Euler(0f, rotationStep * 90f, 0f);
            return instance;
        }

        internal static GameObject PlaceFaceInstance(
            GridLevel grid, GameObject prefab, Vector3Int cell, CellFace face, int rotationStep,
            AuthoredAxis facing, AuthoredAxis up, string undoName)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, grid.transform);
            Undo.RegisterCreatedObjectUndo(instance, undoName);
            TryGetFacePlacement(
                prefab, cell, face, rotationStep, facing, up, grid.CellSize,
                out var rotation, out var position, out _);
            instance.transform.localPosition = position;
            instance.transform.localRotation = rotation;
            return instance;
        }

        // Grid-local pose and bounds a prefab takes when painted on a face.
        // Wall-convention pieces (identity basis) keep their pivot at the face
        // anchor. Basis-rotated pieces recover the face pivot convention from
        // their rotated bounds: flush against the plane extending outward,
        // bottom-anchored on vertical faces, centered on remaining axes.
        // Returns false when the prefab has no renderers; bounds then collapse
        // to the anchor and the pose is pivot-anchored.
        internal static bool TryGetFacePlacement(
            GameObject prefab, Vector3Int cell, CellFace face, int rotationStep,
            AuthoredAxis facing, AuthoredAxis up, Vector3 cellSize,
            out Quaternion rotation, out Vector3 position, out Bounds bounds)
        {
            var basis = GridMath.AuthoredBasis(facing, up);
            rotation = GridMath.FaceRotation(face, rotationStep) * basis;
            position = GridMath.FaceAnchorLocal(cell, face, cellSize);
            if (!TryGetPrefabBounds(prefab, out var prefabBounds))
            {
                bounds = new Bounds(position, Vector3.zero);
                return false;
            }

            bounds = RotatedBoundsAt(prefabBounds, rotation, position);
            if (basis == Quaternion.identity)
            {
                return true;
            }

            var offset = FaceAlignmentOffset(bounds, position, face);
            position += offset;
            bounds.center += offset;
            return true;
        }

        private static Vector3 FaceAlignmentOffset(Bounds placed, Vector3 anchor, CellFace face)
        {
            var normal = GridMath.FaceNormal(face);
            var verticalFace = face != CellFace.NegY && face != CellFace.PosY;
            var offset = Vector3.zero;
            for (var axis = 0; axis < 3; axis++)
            {
                if (normal[axis] > 0)
                {
                    offset[axis] = anchor[axis] - placed.min[axis];
                }
                else if (normal[axis] < 0)
                {
                    offset[axis] = anchor[axis] - placed.max[axis];
                }
                else if (axis == 1 && verticalFace)
                {
                    offset[axis] = anchor[axis] - placed.min[axis];
                }
                else
                {
                    offset[axis] = anchor[axis] - placed.center[axis];
                }
            }
            return offset;
        }

        private static Bounds RotatedBoundsAt(Bounds localBounds, Quaternion rotation, Vector3 position)
        {
            var bounds = new Bounds(position + rotation * localBounds.center, Vector3.zero);
            for (var i = 0; i < 8; i++)
            {
                var corner = localBounds.center + Vector3.Scale(localBounds.extents, new Vector3(
                    (i & 1) == 0 ? -1f : 1f,
                    (i & 2) == 0 ? -1f : 1f,
                    (i & 4) == 0 ? -1f : 1f));
                bounds.Encapsulate(position + rotation * corner);
            }
            return bounds;
        }

        private static readonly Dictionary<GameObject, Bounds> PrefabBounds = new();
        private static readonly HashSet<GameObject> RendererlessPrefabs = new();

        private static bool TryGetPrefabBounds(GameObject prefab, out Bounds bounds)
        {
            bounds = default;
            if (prefab == null || RendererlessPrefabs.Contains(prefab))
            {
                return false;
            }
            if (PrefabBounds.TryGetValue(prefab, out bounds))
            {
                return true;
            }
            if (FootprintDetector.TryMeasureLocalBounds(prefab, out bounds))
            {
                PrefabBounds[prefab] = bounds;
                return true;
            }
            RendererlessPrefabs.Add(prefab);
            return false;
        }

        // Any import or delete can reshape a prefab's bounds through its model,
        // material, or nested-prefab dependencies; re-measuring is cheap, so the
        // whole cache drops rather than tracking dependencies.
        private sealed class PrefabBoundsInvalidator : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(
                string[] imported, string[] deleted, string[] moved, string[] movedFrom)
            {
                if (imported.Length == 0 && deleted.Length == 0)
                {
                    return;
                }
                PrefabBounds.Clear();
                RendererlessPrefabs.Clear();
            }
        }

        private static bool HasPaintablePayload(KitPaletteEntry entry)
        {
            return entry.ruleTile != null
                ? entry.ruleTile.defaultPrefab != null || entry.prefab != null
                : entry.prefab != null;
        }

        private static bool CanReplace(GridLevel grid, Vector3Int anchor, Vector3Int footprint)
        {
            foreach (var cell in GridMath.FootprintCells(anchor, footprint))
            {
                if (!grid.InBounds(cell))
                {
                    return false;
                }
            }
            var anchors = OverlappingAnchors(grid, anchor, footprint);
            if (anchors.Count != 1)
            {
                return false;
            }
            return footprint == Vector3Int.one || anchors[0] == anchor;
        }

        private static List<Vector3Int> OverlappingAnchors(GridLevel grid, Vector3Int anchor, Vector3Int footprint)
        {
            var anchors = new List<Vector3Int>();
            foreach (var cell in GridMath.FootprintCells(anchor, footprint))
            {
                if (grid.TryGetContentAnchor(cell, out var pieceAnchor) && !anchors.Contains(pieceAnchor))
                {
                    anchors.Add(pieceAnchor);
                }
            }
            return anchors;
        }
    }
}
