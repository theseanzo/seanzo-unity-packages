using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Seanzo.LevelDesign.Editor
{
    [EditorTool("Grid Paint", typeof(GridLevel))]
    public sealed class GridPaintTool : EditorTool
    {
        private const int FloodPreviewCap = 256;

        private static readonly Vector3Int NoCell = new(int.MinValue, int.MinValue, int.MinValue);
        private static readonly Color PlaceableColor = new(0.3f, 1f, 0.4f, 0.9f);
        private static readonly Color BlockedColor = new(1f, 0.3f, 0.3f, 0.9f);
        private static readonly Color EraseColor = new(1f, 0.5f, 0.2f, 0.9f);
        private static readonly Vector3[] FaceCorners = new Vector3[4];

        private Vector3Int lastOpCell;
        private bool hasHover;
        private Vector3Int hoverCell;
        private bool boxDragging;
        private Vector3Int boxStartCell;

        public override GUIContent toolbarIcon => EditorGUIUtility.IconContent("Grid.PaintTool");

        public override void OnActivated()
        {
            if (target is GridLevel grid)
            {
                GridPaintOperations.PruneMissing(grid);
            }
        }

        public override void OnToolGUI(EditorWindow window)
        {
            if (window is not SceneView sceneView || target is not GridLevel grid)
            {
                return;
            }

            var e = Event.current;
            var plane = PaintPlaneState.ActivePlane;

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            hasHover = TryGetCellUnderMouse(grid, plane, e.mousePosition, out hoverCell);

            HandleKeys(e, sceneView);
            switch (PaintSession.ToolMode)
            {
                case PaintToolMode.Brush:
                    HandleBrushMouse(grid, plane, e, e.shift);
                    break;
                case PaintToolMode.BoxFill:
                    HandleBoxMouse(grid, plane, e);
                    break;
                case PaintToolMode.FloodFill:
                    HandleFloodMouse(grid, plane, e);
                    break;
                case PaintToolMode.Erase:
                    HandleBrushMouse(grid, plane, e, true);
                    break;
            }

            if (e.type == EventType.Repaint)
            {
                DrawPreview(grid, plane, e);
            }
            if (e.type == EventType.MouseMove)
            {
                sceneView.Repaint();
            }
        }

        private static bool TryGetCellUnderMouse(GridLevel grid, PaintPlane plane, Vector2 mousePosition, out Vector3Int cell)
        {
            cell = default;
            var ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            var toLocal = grid.transform.worldToLocalMatrix;
            var origin = toLocal.MultiplyPoint3x4(ray.origin);
            var direction = toLocal.MultiplyVector(ray.direction);

            var axisN = plane.NormalAxis;
            var planeCoord = plane.index * grid.CellSize[axisN];
            if (Mathf.Approximately(direction[axisN], 0f))
            {
                return false;
            }
            var t = (planeCoord - origin[axisN]) / direction[axisN];
            if (t < 0f)
            {
                return false;
            }

            var hit = origin + direction * t;
            cell = GridMath.LocalToCell(hit, grid.CellSize);
            cell[axisN] = plane.index;
            return true;
        }

        private void HandleKeys(Event e, SceneView sceneView)
        {
            if (e.type != EventType.KeyDown)
            {
                return;
            }
            switch (e.keyCode)
            {
                case KeyCode.LeftBracket:
                    PaintSession.Rotate(-1);
                    break;
                case KeyCode.RightBracket:
                    PaintSession.Rotate(1);
                    break;
                case KeyCode.Minus:
                    PaintPlaneState.StepIndex(-1);
                    break;
                case KeyCode.Equals:
                    PaintPlaneState.StepIndex(1);
                    break;
                case KeyCode.Escape when boxDragging:
                    boxDragging = false;
                    break;
                default:
                    return;
            }
            e.Use();
            sceneView.Repaint();
        }

        private void HandleBrushMouse(GridLevel grid, PaintPlane plane, Event e, bool erase)
        {
            if (e.alt || e.button != 0 || !hasHover)
            {
                return;
            }
            if (e.type != EventType.MouseDown && e.type != EventType.MouseDrag)
            {
                return;
            }
            if (e.type == EventType.MouseDown)
            {
                lastOpCell = NoCell;
            }
            if (e.type == EventType.MouseDrag && hoverCell == lastOpCell)
            {
                e.Use();
                return;
            }

            if (plane.IsHorizontal)
            {
                HandleContentOp(grid, e, erase);
            }
            else if (GridMath.TryVerticalPlaneTarget(plane, hoverCell, PaintPlaneState.FarSide, out var cell, out var face))
            {
                HandleFaceOp(grid, cell, face, e, erase);
            }

            lastOpCell = hoverCell;
            e.Use();
        }

        private void HandleBoxMouse(GridLevel grid, PaintPlane plane, Event e)
        {
            if (e.alt || e.button != 0)
            {
                return;
            }
            switch (e.type)
            {
                case EventType.MouseDown when hasHover:
                    boxDragging = true;
                    boxStartCell = hoverCell;
                    e.Use();
                    break;
                case EventType.MouseDrag when boxDragging:
                    e.Use();
                    break;
                case EventType.MouseUp when boxDragging:
                    boxDragging = false;
                    if (hasHover)
                    {
                        CommitBox(grid, plane, e.shift);
                    }
                    e.Use();
                    break;
            }
        }

        private void CommitBox(GridLevel grid, PaintPlane plane, bool erase)
        {
            if (erase)
            {
                FillOperations.BoxErase(grid, plane, boxStartCell, hoverCell, PaintPlaneState.FarSide);
                return;
            }
            var entry = PaintSession.ActiveEntry;
            if (entry != null)
            {
                FillOperations.BoxFill(
                    grid, entry, plane, boxStartCell, hoverCell,
                    PaintSession.RotationStep, PaintSession.ActiveLayerId, PaintPlaneState.FarSide);
            }
        }

        private void HandleFloodMouse(GridLevel grid, PaintPlane plane, Event e)
        {
            if (e.alt || e.button != 0 || !hasHover || e.type != EventType.MouseDown || e.shift)
            {
                return;
            }
            var entry = PaintSession.ActiveEntry;
            if (entry != null)
            {
                FillOperations.FloodFill(
                    grid, entry, plane, hoverCell,
                    PaintSession.RotationStep, PaintSession.ActiveLayerId, PaintPlaneState.FarSide,
                    out var truncated);
                if (truncated)
                {
                    Debug.LogWarning(
                        $"Flood fill capped at {FillOperations.UnboundedFloodCap} cells. Enable bounds on the GridLevel for full fills.");
                }
            }
            e.Use();
        }

        private void HandleContentOp(GridLevel grid, Event e, bool erase)
        {
            if (e.control || e.command)
            {
                if (e.type == EventType.MouseDown && GridPaintOperations.TryPick(grid, hoverCell, out var prefab, out var rotation))
                {
                    AdoptPick(prefab, rotation);
                }
            }
            else if (erase)
            {
                GridPaintOperations.Erase(grid, hoverCell);
            }
            else
            {
                var entry = PaintSession.ActiveEntry;
                if (entry != null && entry.allowedSlot != SlotKind.Face)
                {
                    GridPaintOperations.Paint(grid, entry, hoverCell, PaintSession.RotationStep, PaintSession.ActiveLayerId);
                }
            }
        }

        private void HandleFaceOp(GridLevel grid, Vector3Int cell, CellFace face, Event e, bool erase)
        {
            if (e.control || e.command)
            {
                if (e.type == EventType.MouseDown && GridPaintOperations.TryPickFace(grid, cell, face, out var prefab, out var rotation))
                {
                    AdoptPick(prefab, rotation);
                }
            }
            else if (erase)
            {
                GridPaintOperations.EraseFace(grid, cell, face);
            }
            else
            {
                var entry = PaintSession.ActiveEntry;
                if (entry != null && entry.allowedSlot != SlotKind.Content)
                {
                    GridPaintOperations.PaintFace(grid, entry, cell, face, PaintSession.RotationStep, PaintSession.ActiveLayerId);
                }
            }
        }

        private static void AdoptPick(GameObject prefab, int rotationStep)
        {
            var palette = PaintSession.Palette;
            if (palette == null)
            {
                return;
            }
            var index = palette.entries.FindIndex(entry => entry.prefab == prefab);
            if (index >= 0)
            {
                PaintSession.SelectEntry(index);
                PaintSession.RotationStep = rotationStep;
            }
        }

        private void DrawPreview(GridLevel grid, PaintPlane plane, Event e)
        {
            if (!hasHover && !boxDragging)
            {
                return;
            }
            using (new Handles.DrawingScope(grid.transform.localToWorldMatrix))
            {
                switch (PaintSession.ToolMode)
                {
                    case PaintToolMode.Brush:
                        DrawBrushPreview(grid, plane, e, e.shift);
                        break;
                    case PaintToolMode.BoxFill:
                        DrawBoxPreview(grid, plane, e);
                        break;
                    case PaintToolMode.FloodFill:
                        DrawFloodPreview(grid, plane, e);
                        break;
                    case PaintToolMode.Erase:
                        DrawBrushPreview(grid, plane, e, true);
                        break;
                }
            }
        }

        private void DrawBrushPreview(GridLevel grid, PaintPlane plane, Event e, bool erase)
        {
            if (!hasHover)
            {
                return;
            }
            if (plane.IsHorizontal)
            {
                if (erase)
                {
                    DrawErasePreview(grid);
                }
                else
                {
                    DrawPaintPreview(grid);
                }
            }
            else if (GridMath.TryVerticalPlaneTarget(plane, hoverCell, PaintPlaneState.FarSide, out var cell, out var face))
            {
                DrawFacePreview(grid, cell, face, erase);
            }
        }

        private void DrawBoxPreview(GridLevel grid, PaintPlane plane, Event e)
        {
            var endCell = hasHover ? hoverCell : boxStartCell;
            var startCell = boxDragging ? boxStartCell : endCell;
            if (!boxDragging && !hasHover)
            {
                return;
            }

            var color = e.shift ? EraseColor : PlaceableColor;
            DrawPlaneRect(grid, plane, startCell, endCell, color);

            if (boxDragging && !e.shift)
            {
                var entry = PaintSession.ActiveEntry;
                if (entry != null && plane.IsHorizontal && entry.allowedSlot != SlotKind.Face)
                {
                    var footprint = GridMath.RotateFootprint(entry.Footprint, PaintSession.RotationStep);
                    Handles.color = PlaceableColor;
                    foreach (var anchor in FillOperations.RectStrideAnchors(plane, startCell, endCell, footprint))
                    {
                        if (grid.CanPlaceContent(anchor, footprint))
                        {
                            DrawCellCube(grid, anchor);
                        }
                    }
                }
            }
        }

        private void DrawFloodPreview(GridLevel grid, PaintPlane plane, Event e)
        {
            if (!hasHover || e.shift)
            {
                return;
            }
            var entry = PaintSession.ActiveEntry;
            if (entry == null)
            {
                return;
            }

            Handles.color = PlaceableColor;
            if (plane.IsHorizontal)
            {
                if (entry.allowedSlot == SlotKind.Face)
                {
                    return;
                }
                var region = FillOperations.ContentFloodRegion(grid, plane, hoverCell, FloodPreviewCap, out _);
                foreach (var cell in region)
                {
                    DrawCellCube(grid, cell);
                }
            }
            else
            {
                if (entry.allowedSlot == SlotKind.Content)
                {
                    return;
                }
                var region = FillOperations.FaceFloodRegion(grid, plane, hoverCell, PaintPlaneState.FarSide, FloodPreviewCap, out _);
                foreach (var planeCell in region)
                {
                    if (GridMath.TryVerticalPlaneTarget(plane, planeCell, PaintPlaneState.FarSide, out var cell, out var face))
                    {
                        GridMath.FaceCornersLocal(cell, face, grid.CellSize, FaceCorners);
                        var fill = PlaceableColor;
                        fill.a = 0.08f;
                        Handles.DrawSolidRectangleWithOutline(FaceCorners, fill, PlaceableColor);
                    }
                }
            }
        }

        private static void DrawPlaneRect(GridLevel grid, PaintPlane plane, Vector3Int cornerA, Vector3Int cornerB, Color color)
        {
            var (axisA, axisB) = plane.InPlaneAxes;
            var axisN = plane.NormalAxis;
            var cellSize = grid.CellSize;

            var (a0, b0) = plane.InPlaneCoords(cornerA);
            var (a1, b1) = plane.InPlaneCoords(cornerB);
            var minA = Mathf.Min(a0, a1);
            var maxA = Mathf.Max(a0, a1) + 1;
            var minB = Mathf.Min(b0, b1);
            var maxB = Mathf.Max(b0, b1) + 1;
            var n = plane.index * cellSize[axisN];

            Vector3 Point(float a, float b)
            {
                var p = Vector3.zero;
                p[axisA] = a * cellSize[axisA];
                p[axisB] = b * cellSize[axisB];
                p[axisN] = n;
                return p;
            }

            var corners = new[]
            {
                Point(minA, minB),
                Point(maxA, minB),
                Point(maxA, maxB),
                Point(minA, maxB)
            };
            var fill = color;
            fill.a = 0.1f;
            Handles.DrawSolidRectangleWithOutline(corners, fill, color);
        }

        private void DrawErasePreview(GridLevel grid)
        {
            Handles.color = EraseColor;
            if (grid.TryGetContent(hoverCell, out var anchor, out var slot))
            {
                foreach (var cell in GridMath.FootprintCells(anchor, slot.footprint))
                {
                    DrawCellCube(grid, cell);
                }
            }
            else
            {
                DrawCellCube(grid, hoverCell);
            }
        }

        private void DrawPaintPreview(GridLevel grid)
        {
            var entry = PaintSession.ActiveEntry;
            if (entry == null)
            {
                Handles.color = BlockedColor;
                DrawCellCube(grid, hoverCell);
                return;
            }
            var footprint = GridMath.RotateFootprint(entry.Footprint, PaintSession.RotationStep);
            var placeable = entry.allowedSlot != SlotKind.Face
                && GridPaintOperations.CanPaint(grid, entry, hoverCell, PaintSession.RotationStep);
            Handles.color = placeable ? PlaceableColor : BlockedColor;
            foreach (var cell in GridMath.FootprintCells(hoverCell, footprint))
            {
                DrawCellCube(grid, cell);
            }
        }

        private static void DrawFacePreview(GridLevel grid, Vector3Int cell, CellFace face, bool erase)
        {
            Color color;
            if (erase)
            {
                color = grid.GetFace(cell, face) != null ? EraseColor : BlockedColor;
            }
            else
            {
                var entry = PaintSession.ActiveEntry;
                var placeable = entry != null
                    && entry.allowedSlot != SlotKind.Content
                    && GridPaintOperations.CanPaintFace(grid, entry, cell);
                color = placeable ? PlaceableColor : BlockedColor;
            }

            GridMath.FaceCornersLocal(cell, face, grid.CellSize, FaceCorners);
            var fill = color;
            fill.a = 0.12f;
            Handles.DrawSolidRectangleWithOutline(FaceCorners, fill, color);

            if (!erase)
            {
                DrawFaceGhostBounds(grid, cell, face, color);
            }
        }

        // Wire box of the actual bounds the piece will occupy, so oversized or
        // reoriented pieces read correctly against the one-cell face rect.
        // Rule tiles resolve against current neighbor occupancy, the same way
        // painting will.
        private static void DrawFaceGhostBounds(GridLevel grid, Vector3Int cell, CellFace face, Color color)
        {
            var entry = PaintSession.ActiveEntry;
            if (entry == null)
            {
                return;
            }
            var prefab = entry.prefab;
            var rotationStep = PaintSession.RotationStep;
            if (entry.ruleTile != null)
            {
                RuleMatcher.Resolve(
                    entry.ruleTile, RuleResolution.FaceOccupancy(grid, cell, face), out prefab, out rotationStep);
                if (prefab == null)
                {
                    prefab = entry.prefab;
                }
            }
            if (prefab == null)
            {
                return;
            }
            if (!GridPaintOperations.TryGetFacePlacement(
                    prefab, cell, face, rotationStep, entry.authoredFacing, entry.authoredUp,
                    grid.CellSize, out _, out _, out var ghost))
            {
                return;
            }
            Handles.color = color;
            Handles.DrawWireCube(ghost.center, ghost.size);
        }

        private static void DrawCellCube(GridLevel grid, Vector3Int cell)
        {
            Handles.DrawWireCube(GridMath.CellCenterLocal(cell, grid.CellSize), grid.CellSize);
        }
    }
}
