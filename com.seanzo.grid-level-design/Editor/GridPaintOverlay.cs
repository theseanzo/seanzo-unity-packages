using System.Linq;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace Seanzo.LevelDesign.Editor
{
    [Overlay(typeof(SceneView), "Seanzo Grid Paint", true)]
    public sealed class GridPaintOverlay : Overlay
    {
        private static readonly string[] OrientationLabels = { "XZ", "XY", "YZ" };
        private static readonly string[] ToolLabels = { "Brush", "Box", "Flood", "Erase" };

        public override VisualElement CreatePanelContent()
        {
            return new IMGUIContainer(DrawContents)
            {
                style = { minWidth = 240 }
            };
        }

        private void DrawContents()
        {
            var grid = ActiveGrid();
            if (grid == null)
            {
                EditorGUILayout.LabelField("Select a GridLevel to paint.", EditorStyles.miniLabel);
                return;
            }

            DrawToolControls();
            EditorGUILayout.Space(4f);
            DrawPlaneControls();
            EditorGUILayout.Space(4f);
            DrawLayerControls(grid);
            EditorGUILayout.Space(4f);
            DrawViewControls();
        }

        private static GridLevel ActiveGrid()
        {
            var selected = Selection.activeGameObject;
            return selected != null ? selected.GetComponentInParent<GridLevel>() : null;
        }

        private static void DrawToolControls()
        {
            EditorGUILayout.LabelField("Tool", EditorStyles.boldLabel);
            var mode = (PaintToolMode)GUILayout.Toolbar((int)PaintSession.ToolMode, ToolLabels);
            if (mode != PaintSession.ToolMode)
            {
                PaintSession.ToolMode = mode;
                SceneView.RepaintAll();
            }
        }

        private static void DrawPlaneControls()
        {
            // Snapshot once and apply at the end: mutating state mid-draw changes the
            // control count between the Layout and event passes and breaks the container.
            var plane = PaintPlaneState.ActivePlane;
            var farSide = PaintPlaneState.FarSide;
            var ghosts = PaintPlaneState.GhostPlanes;

            EditorGUILayout.LabelField("Paint Plane", EditorStyles.boldLabel);
            var newOrientation = (PlaneOrientation)GUILayout.Toolbar((int)plane.orientation, OrientationLabels);

            EditorGUILayout.BeginHorizontal();
            var indexLabel = plane.IsHorizontal ? "Elevation" : "Row";
            var indexDelta = 0;
            if (GUILayout.Button("-", GUILayout.Width(24f)))
            {
                indexDelta = -1;
            }
            var newIndex = EditorGUILayout.IntField(indexLabel, plane.index);
            if (GUILayout.Button("+", GUILayout.Width(24f)))
            {
                indexDelta = 1;
            }
            EditorGUILayout.EndHorizontal();

            var newFarSide = farSide;
            using (new EditorGUI.DisabledScope(plane.IsHorizontal))
            {
                newFarSide = EditorGUILayout.ToggleLeft("Paint far side of plane", farSide);
            }
            var newGhosts = EditorGUILayout.ToggleLeft("Show ghost planes", ghosts);
            EditorGUILayout.LabelField("Slice: - / =    Rotate: [ / ]", EditorStyles.miniLabel);

            var newPlane = new PaintPlane(newOrientation, newIndex + indexDelta);
            if (!newPlane.Equals(plane) || newFarSide != farSide || newGhosts != ghosts)
            {
                if (newOrientation != plane.orientation)
                {
                    PaintPlaneState.SetOrientation(newOrientation);
                }
                else
                {
                    PaintPlaneState.ActivePlane = newPlane;
                }
                PaintPlaneState.FarSide = newFarSide;
                PaintPlaneState.GhostPlanes = newGhosts;
                SceneView.RepaintAll();
            }
        }

        private static void DrawLayerControls(GridLevel grid)
        {
            EditorGUILayout.LabelField("Layers", EditorStyles.boldLabel);

            var layers = grid.Layers;
            var names = layers.Select(l => l.name).ToArray();
            var activeIndex = 0;
            for (var i = 0; i < layers.Count; i++)
            {
                if (layers[i].id == PaintSession.ActiveLayerId)
                {
                    activeIndex = i;
                    break;
                }
            }
            var pickedIndex = EditorGUILayout.Popup("Active Layer", activeIndex, names);
            if (pickedIndex >= 0 && pickedIndex < layers.Count)
            {
                PaintSession.ActiveLayerId = layers[pickedIndex].id;
            }

            foreach (var layer in layers)
            {
                var visible = EditorGUILayout.ToggleLeft(layer.name, layer.visible);
                if (visible != layer.visible)
                {
                    Undo.RecordObject(grid, "Toggle Layer Visibility");
                    layer.visible = visible;
                    EditorUtility.SetDirty(grid);
                    LayerVisibilityUtility.Apply(grid);
                    SceneView.RepaintAll();
                }
            }

            if (GUILayout.Button("Add Layer"))
            {
                Undo.RecordObject(grid, "Add Layer");
                grid.AddLayer($"Layer {grid.Layers.Count}");
                EditorUtility.SetDirty(grid);
            }
        }

        private void DrawViewControls()
        {
            EditorGUILayout.LabelField("View", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Top"))
            {
                SnapView(Quaternion.Euler(90f, 0f, 0f));
            }
            if (GUILayout.Button("Front"))
            {
                SnapView(Quaternion.Euler(0f, 180f, 0f));
            }
            if (GUILayout.Button("Right"))
            {
                SnapView(Quaternion.Euler(0f, -90f, 0f));
            }
            if (GUILayout.Button("Plane"))
            {
                SnapView(PlaneAlignedRotation());
            }
            EditorGUILayout.EndHorizontal();

            var sceneView = containerWindow as SceneView;
            if (sceneView != null)
            {
                var ortho = GUILayout.Toggle(sceneView.orthographic, "Orthographic");
                if (ortho != sceneView.orthographic)
                {
                    sceneView.orthographic = ortho;
                }
            }
        }

        private static Quaternion PlaneAlignedRotation()
        {
            var plane = PaintPlaneState.ActivePlane;
            var farSide = PaintPlaneState.FarSide;
            return plane.orientation switch
            {
                PlaneOrientation.XZ => Quaternion.Euler(90f, 0f, 0f),
                PlaneOrientation.XY => farSide ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity,
                _ => farSide ? Quaternion.Euler(0f, -90f, 0f) : Quaternion.Euler(0f, 90f, 0f)
            };
        }

        private void SnapView(Quaternion rotation)
        {
            if (containerWindow is SceneView sceneView)
            {
                sceneView.LookAt(sceneView.pivot, rotation, sceneView.size, sceneView.orthographic);
            }
        }
    }
}
