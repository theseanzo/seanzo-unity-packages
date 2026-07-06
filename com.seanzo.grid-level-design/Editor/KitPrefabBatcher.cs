using System.IO;
using UnityEditor;
using UnityEngine;

namespace Seanzo.LevelDesign.Editor
{
    public static class KitPrefabBatcher
    {
        private const string MenuPath = "Tools/Seanzo/Create Kit Prefabs From Selection";

        [MenuItem(MenuPath, true)]
        private static bool Validate()
        {
            return SelectedModels().Length > 0;
        }

        [MenuItem(MenuPath)]
        private static void Run()
        {
            var models = SelectedModels();
            var absolute = EditorUtility.SaveFolderPanel(
                "Kit prefab output folder", "Assets", "Kit");
            if (string.IsNullOrEmpty(absolute))
            {
                return;
            }
            var outputFolder = FileUtil.GetProjectRelativePath(absolute);
            if (string.IsNullOrEmpty(outputFolder) || !outputFolder.StartsWith("Assets"))
            {
                EditorUtility.DisplayDialog(
                    "Create Kit Prefabs", "The output folder must be inside this project's Assets folder.", "OK");
                return;
            }
            if (!AssetDatabase.IsValidFolder(outputFolder))
            {
                Directory.CreateDirectory(absolute);
                AssetDatabase.Refresh();
            }

            var created = 0;
            var skipped = 0;
            try
            {
                for (var i = 0; i < models.Length; i++)
                {
                    var model = models[i];
                    EditorUtility.DisplayProgressBar(
                        "Creating kit prefabs", model.name, (float)i / models.Length);

                    var path = $"{outputFolder}/{model.name}.prefab";
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                    {
                        skipped++;
                        continue;
                    }

                    CreateWrappedPrefab(model, path, Quaternion.identity);
                    created++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log($"Kit prefabs: {created} created, {skipped} already existed in {outputFolder}.");
        }

        internal static GameObject CreateWrappedPrefab(GameObject model, string path, Quaternion childRotation)
        {
            var root = new GameObject(model.name);
            try
            {
                var child = (GameObject)PrefabUtility.InstantiatePrefab(model);
                child.transform.SetParent(root.transform, false);
                child.transform.localPosition = Vector3.zero;
                child.transform.localRotation = childRotation;
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        internal static GameObject[] SelectedModels()
        {
            var candidates = Selection.GetFiltered<GameObject>(SelectionMode.DeepAssets);
            return System.Array.FindAll(
                candidates,
                go => PrefabUtility.GetPrefabAssetType(go) == PrefabAssetType.Model
                    && PrefabUtility.IsPartOfPrefabAsset(go)
                    && go.transform.parent == null);
        }
    }
}
