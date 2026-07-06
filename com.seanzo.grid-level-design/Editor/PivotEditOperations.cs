using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Seanzo.LevelDesign.Editor
{
    public static class PivotEditOperations
    {
        public static bool CanEdit(PrefabStage stage, out string reason)
        {
            reason = null;
            if (stage == null)
            {
                reason = "Open a prefab to edit its pivot.";
                return false;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                reason = "Pivot editing is disabled in play mode.";
                return false;
            }
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(stage.assetPath);
            if (asset == null)
            {
                reason = "The open stage has no prefab asset.";
                return false;
            }
            if (PrefabUtility.IsPartOfImmutablePrefab(asset))
            {
                reason = PrefabUtility.GetPrefabAssetType(asset) == PrefabAssetType.Model
                    ? "Model assets cannot be saved over. Wrap the model via Tools > Seanzo > Create Kit Prefabs From Selection, then edit the wrapper prefab."
                    : "This prefab is immutable and cannot be edited.";
                return false;
            }
            if (stage.prefabContentsRoot.GetComponent<Renderer>() != null)
            {
                reason = "The prefab root carries a renderer, so there is no child to offset. Wrap it via Tools > Seanzo > Create Kit Prefabs From Selection.";
                return false;
            }
            if (stage.prefabContentsRoot.transform.childCount == 0)
            {
                reason = "The prefab root has no children to offset.";
                return false;
            }
            return true;
        }

        // Makes the pending pivot the new origin by offsetting the root's direct
        // children, then persists the stage contents to the prefab asset so every
        // instance updates.
        public static bool Apply(PrefabStage stage)
        {
            if (!CanEdit(stage, out _))
            {
                return false;
            }
            var pivot = PivotEditSession.GetPivot(stage.assetPath);
            if (pivot == Vector3.zero)
            {
                return false;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Apply Pivot");
            var group = Undo.GetCurrentGroup();

            ApplyOffset(stage.prefabContentsRoot, pivot);
            PivotEditSession.Reset(stage.assetPath);
            PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, stage.assetPath);

            Undo.CollapseUndoOperations(group);
            return true;
        }

        public static void ApplyOffset(GameObject root, Vector3 pivotLocal)
        {
            foreach (Transform child in root.transform)
            {
                Undo.RecordObject(child, "Apply Pivot");
                child.localPosition -= pivotLocal;
            }
        }
    }
}
