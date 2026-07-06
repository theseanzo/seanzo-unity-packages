using System.IO;
using UnityEditor;
using UnityEngine;

namespace Seanzo.LevelDesign.Editor
{
    // Bulk Blender Z-up correction. Selected kit prefabs get their wrapped children
    // rotated in place; selected model assets get wrapped into kit prefabs with the
    // correction applied (or their existing wrapper corrected).
    public static class KitAxisCorrector
    {
        private const string FixMenuPath = "Tools/Seanzo/Fix Blender Z-Up For Selection";
        private const string UndoMenuPath = "Tools/Seanzo/Undo Blender Z-Up Fix For Selection";

        [MenuItem(FixMenuPath, true)]
        [MenuItem(UndoMenuPath, true)]
        private static bool Validate()
        {
            return SelectedRegularPrefabs().Length > 0 || KitPrefabBatcher.SelectedModels().Length > 0;
        }

        [MenuItem(FixMenuPath)]
        private static void Fix()
        {
            Run(AxisCorrectionUtility.ZUpFix);
        }

        [MenuItem(UndoMenuPath)]
        private static void UndoFix()
        {
            Run(Quaternion.Inverse(AxisCorrectionUtility.ZUpFix));
        }

        private static void Run(Quaternion rotation)
        {
            var prefabs = SelectedRegularPrefabs();
            var models = KitPrefabBatcher.SelectedModels();

            var corrected = 0;
            var skipped = 0;
            try
            {
                for (var i = 0; i < prefabs.Length; i++)
                {
                    EditorUtility.DisplayProgressBar(
                        "Axis correction", prefabs[i].name, (float)i / prefabs.Length);
                    var path = AssetDatabase.GetAssetPath(prefabs[i]);
                    if (AxisCorrectionUtility.CorrectPrefabAsset(path, rotation))
                    {
                        corrected++;
                    }
                    else
                    {
                        skipped++;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (models.Length > 0)
            {
                CorrectModels(models, rotation, ref corrected, ref skipped);
            }

            Debug.Log($"Axis correction: {corrected} corrected, {skipped} skipped.");
        }

        private static void CorrectModels(GameObject[] models, Quaternion rotation, ref int corrected, ref int skipped)
        {
            var absolute = EditorUtility.SaveFolderPanel(
                "Kit prefab output folder for selected models", "Assets", "Kit");
            if (string.IsNullOrEmpty(absolute))
            {
                skipped += models.Length;
                return;
            }
            var outputFolder = FileUtil.GetProjectRelativePath(absolute);
            if (string.IsNullOrEmpty(outputFolder) || !outputFolder.StartsWith("Assets"))
            {
                EditorUtility.DisplayDialog(
                    "Axis correction", "The output folder must be inside this project's Assets folder.", "OK");
                skipped += models.Length;
                return;
            }
            if (!AssetDatabase.IsValidFolder(outputFolder))
            {
                Directory.CreateDirectory(absolute);
                AssetDatabase.Refresh();
            }

            try
            {
                for (var i = 0; i < models.Length; i++)
                {
                    var model = models[i];
                    EditorUtility.DisplayProgressBar(
                        "Axis correction", model.name, (float)i / models.Length);

                    var path = $"{outputFolder}/{model.name}.prefab";
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                    {
                        if (AxisCorrectionUtility.CorrectPrefabAsset(path, rotation))
                        {
                            corrected++;
                        }
                        else
                        {
                            skipped++;
                        }
                        continue;
                    }
                    KitPrefabBatcher.CreateWrappedPrefab(model, path, rotation);
                    corrected++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static GameObject[] SelectedRegularPrefabs()
        {
            var candidates = Selection.GetFiltered<GameObject>(SelectionMode.DeepAssets);
            return System.Array.FindAll(
                candidates,
                go => PrefabUtility.GetPrefabAssetType(go) == PrefabAssetType.Regular
                    && PrefabUtility.IsPartOfPrefabAsset(go)
                    && go.transform.parent == null);
        }
    }
}
