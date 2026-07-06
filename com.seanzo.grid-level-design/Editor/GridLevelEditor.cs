using UnityEditor;
using UnityEngine;

namespace Seanzo.LevelDesign.Editor
{
    [CustomEditor(typeof(GridLevel))]
    public sealed class GridLevelEditor : UnityEditor.Editor
    {
        private const int DefaultHalfExtent = 8;

        private static readonly Color LineColor = new(1f, 1f, 1f, 0.18f);
        private static readonly Color BorderColor = new(0.4f, 0.8f, 1f, 0.9f);
        private static readonly Color GhostLineColor = new(1f, 1f, 1f, 0.05f);
        private static readonly Color GhostBorderColor = new(0.4f, 0.8f, 1f, 0.25f);

        private void OnEnable()
        {
            if (target is GridLevel grid)
            {
                GridPaintOperations.PruneMissing(grid);
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var grid = (GridLevel)target;
            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.LabelField("Content cells", grid.ContentCount.ToString());
                EditorGUILayout.LabelField("Face cells", grid.FaceCount.ToString());
                EditorGUILayout.LabelField("Active plane", PaintPlaneState.ActivePlane.ToString());
            }

            var missing = grid.CountMissingInstances();
            if (missing > 0)
            {
                EditorGUILayout.HelpBox(
                    $"{missing} cell(s) reference instances that were deleted outside the tool.",
                    MessageType.Warning);
                if (GUILayout.Button("Prune Missing Cells"))
                {
                    GridPaintOperations.PruneMissing(grid);
                }
            }
        }

        private void OnSceneGUI()
        {
            var grid = (GridLevel)target;
            var plane = PaintPlaneState.ActivePlane;
            using (new Handles.DrawingScope(grid.transform.localToWorldMatrix))
            {
                if (PaintPlaneState.GhostPlanes)
                {
                    foreach (PlaneOrientation orientation in System.Enum.GetValues(typeof(PlaneOrientation)))
                    {
                        if (orientation != plane.orientation)
                        {
                            DrawPlaneGrid(grid, PaintPlaneState.PlaneFor(orientation), GhostLineColor, GhostBorderColor);
                        }
                    }
                }
                DrawPlaneGrid(grid, plane, LineColor, BorderColor);
            }
        }

        private static void DrawPlaneGrid(GridLevel grid, PaintPlane plane, Color lineColor, Color borderColor)
        {
            var (axisA, axisB) = plane.InPlaneAxes;
            var axisN = plane.NormalAxis;
            var cellSize = grid.CellSize;

            int minA, maxA, minB, maxB;
            if (grid.UseBounds)
            {
                var b = grid.Bounds;
                minA = b.min[axisA];
                maxA = b.max[axisA];
                minB = b.min[axisB];
                maxB = b.max[axisB];
            }
            else
            {
                minA = -DefaultHalfExtent;
                maxA = DefaultHalfExtent;
                minB = -DefaultHalfExtent;
                maxB = DefaultHalfExtent;
            }

            var n = plane.index * cellSize[axisN];

            Vector3 Point(float a, float b)
            {
                var p = Vector3.zero;
                p[axisA] = a * cellSize[axisA];
                p[axisB] = b * cellSize[axisB];
                p[axisN] = n;
                return p;
            }

            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            Handles.color = lineColor;
            for (var a = minA + 1; a < maxA; a++)
            {
                Handles.DrawLine(Point(a, minB), Point(a, maxB));
            }
            for (var b = minB + 1; b < maxB; b++)
            {
                Handles.DrawLine(Point(minA, b), Point(maxA, b));
            }

            Handles.color = borderColor;
            Handles.DrawLine(Point(minA, minB), Point(maxA, minB));
            Handles.DrawLine(Point(maxA, minB), Point(maxA, maxB));
            Handles.DrawLine(Point(maxA, maxB), Point(minA, maxB));
            Handles.DrawLine(Point(minA, maxB), Point(minA, minB));
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
        }
    }
}
