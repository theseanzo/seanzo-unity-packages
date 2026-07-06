using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Seanzo.LevelDesign.Editor
{
    [EditorTool("Pivot Editor")]
    public sealed class PivotEditorTool : EditorTool
    {
        private static readonly Color BoundsColor = new(0.4f, 0.8f, 1f, 0.9f);
        private static readonly Color PivotColor = new(0.3f, 1f, 0.4f, 0.9f);
        private static readonly Color FrameColor = new(0.4f, 1f, 0.9f, 0.9f);

        public override GUIContent toolbarIcon => EditorGUIUtility.IconContent("ToolHandlePivot");

        public override bool IsAvailable()
        {
            return PrefabStageUtility.GetCurrentPrefabStage() != null;
        }

        public override void OnToolGUI(EditorWindow window)
        {
            if (window is not SceneView)
            {
                return;
            }
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (!PivotEditOperations.CanEdit(stage, out _))
            {
                return;
            }
            var root = stage.prefabContentsRoot;
            if (!PivotMath.TryCombinedBounds(root, out var bounds))
            {
                return;
            }

            var pivot = PivotEditSession.GetPivot(stage.assetPath);

            using (new Handles.DrawingScope(root.transform.localToWorldMatrix))
            {
                Handles.color = BoundsColor;
                Handles.DrawWireCube(bounds.center, bounds.size);

                EditorGUI.BeginChangeCheck();
                var newPivot = Handles.PositionHandle(pivot, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    PivotEditSession.SetPivot(stage.assetPath, newPivot);
                }

                Handles.color = PivotColor;
                Handles.SphereHandleCap(
                    0, newPivot, Quaternion.identity,
                    HandleUtility.GetHandleSize(newPivot) * 0.08f, EventType.Repaint);

                DrawPlacementFrame(newPivot);
            }
        }

        // Draws the cell the piece will occupy, anchored at the pending pivot exactly
        // the way grid painting anchors the prefab origin: floors anchor at the cell's
        // bottom-center; walls anchor at the bottom-center of the boundary face with
        // local +Z pointing out of the cell.
        private static void DrawPlacementFrame(Vector3 pivot)
        {
            var module = PivotEditSession.PreviewModuleSize;
            Handles.color = FrameColor;
            if (PivotEditSession.PreviewMode == PivotPreviewMode.Floor)
            {
                var center = pivot + new Vector3(0f, module.y * 0.5f, 0f);
                Handles.DrawWireCube(center, module);
                return;
            }

            var corners = new[]
            {
                pivot + new Vector3(-module.x * 0.5f, 0f, 0f),
                pivot + new Vector3(-module.x * 0.5f, module.y, 0f),
                pivot + new Vector3(module.x * 0.5f, module.y, 0f),
                pivot + new Vector3(module.x * 0.5f, 0f, 0f)
            };
            var fill = FrameColor;
            fill.a = 0.06f;
            Handles.DrawSolidRectangleWithOutline(corners, fill, FrameColor);
            var cellCenter = pivot + new Vector3(0f, module.y * 0.5f, -module.z * 0.5f);
            Handles.DrawWireCube(cellCenter, module);
        }
    }
}
