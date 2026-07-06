using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Seanzo.LevelDesign.Editor
{
    [Overlay(typeof(SceneView), "Seanzo Pivot Editor")]
    public sealed class PivotOverlay : Overlay, ITransientOverlay
    {
        private static readonly string[] StepLabels = { "Min", "Center", "Max" };
        private static readonly string[] PreviewLabels = { "Floor", "Wall" };

        public bool visible => PrefabStageUtility.GetCurrentPrefabStage() != null;

        public override VisualElement CreatePanelContent()
        {
            return new IMGUIContainer(DrawContents)
            {
                style = { minWidth = 240 }
            };
        }

        private static void DrawContents()
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (!PivotEditOperations.CanEdit(stage, out var reason))
            {
                EditorGUILayout.HelpBox(reason ?? string.Empty, MessageType.Info);
                return;
            }

            if (ToolManager.activeToolType != typeof(PivotEditorTool))
            {
                if (GUILayout.Button("Activate Pivot Tool"))
                {
                    ToolManager.SetActiveTool<PivotEditorTool>();
                }
            }

            var pivot = PivotEditSession.GetPivot(stage.assetPath);
            EditorGUI.BeginChangeCheck();
            var newPivot = EditorGUILayout.Vector3Field("Pivot (local)", pivot);
            if (EditorGUI.EndChangeCheck())
            {
                PivotEditSession.SetPivot(stage.assetPath, newPivot);
                SceneView.RepaintAll();
            }

            EditorGUILayout.LabelField("Placement Preview", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var mode = (PivotPreviewMode)GUILayout.Toolbar((int)PivotEditSession.PreviewMode, PreviewLabels);
            var module = EditorGUILayout.Vector3Field(
                new GUIContent("Module Size", "Size of the placement frame; defaults to the active palette's module size."),
                PivotEditSession.PreviewModuleSize);
            if (EditorGUI.EndChangeCheck())
            {
                PivotEditSession.PreviewMode = mode;
                PivotEditSession.PreviewModuleSize = module;
                SceneView.RepaintAll();
            }
            EditorGUILayout.LabelField(
                "Drag the pivot until the frame wraps the mesh as it should sit in the cell.",
                EditorStyles.miniLabel);

            var hasBounds = PivotMath.TryCombinedBounds(stage.prefabContentsRoot, out var bounds);
            using (new EditorGUI.DisabledScope(!hasBounds))
            {
                EditorGUILayout.LabelField("Snap", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Fit Floor", "Bottom-center of the bounds: the piece stands centered in its cell.")))
                {
                    PivotEditSession.PreviewMode = PivotPreviewMode.Floor;
                    SetPivot(stage, PivotMath.SnapPoint(bounds, 1, 0, 1));
                }
                if (GUILayout.Button(new GUIContent("Fit Wall", "Bottom-center of the +Z bounds face: the thickness tucks inside the painted cell.")))
                {
                    PivotEditSession.PreviewMode = PivotPreviewMode.Wall;
                    SetPivot(stage, PivotMath.SnapPoint(bounds, 1, 0, 2));
                }
                EditorGUILayout.EndHorizontal();
                DrawAxisSnapRow(stage, bounds, 0, "X");
                DrawAxisSnapRow(stage, bounds, 1, "Y");
                DrawAxisSnapRow(stage, bounds, 2, "Z");
            }

            EditorGUILayout.Space(4f);
            var pending = PivotEditSession.GetPivot(stage.assetPath);
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(pending == Vector3.zero))
            {
                if (GUILayout.Button("Apply"))
                {
                    PivotEditOperations.Apply(stage);
                    SceneView.RepaintAll();
                }
                if (GUILayout.Button("Revert"))
                {
                    PivotEditSession.Reset(stage.assetPath);
                    SceneView.RepaintAll();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(
                "Apply offsets the root's children and saves the prefab.", EditorStyles.miniLabel);
        }

        private static void DrawAxisSnapRow(PrefabStage stage, Bounds bounds, int axis, string label)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(16f));
            for (var step = 0; step < 3; step++)
            {
                if (GUILayout.Button(StepLabels[step]))
                {
                    var pivot = PivotEditSession.GetPivot(stage.assetPath);
                    var target = PivotMath.SnapPoint(bounds, step, step, step);
                    pivot[axis] = target[axis];
                    SetPivot(stage, pivot);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void SetPivot(PrefabStage stage, Vector3 pivot)
        {
            PivotEditSession.SetPivot(stage.assetPath, pivot);
            SceneView.RepaintAll();
        }
    }
}
