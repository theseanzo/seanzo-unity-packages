using UnityEditor;
using UnityEngine;

namespace Seanzo.LevelDesign.Editor
{
    public static class AxisCorrectionUtility
    {
        // Stands a Blender Z-up mesh upright: maps the imported +Z (Blender up) to +Y.
        public static readonly Quaternion ZUpFix = Quaternion.Euler(-90f, 0f, 0f);

        // Rotates the root's direct children about the root origin. Applied to a kit
        // prefab wrapper, the correction lives on the wrapped child and therefore
        // survives model re-import.
        public static void RotateChildren(GameObject root, Quaternion rotation)
        {
            foreach (Transform child in root.transform)
            {
                Undo.RecordObject(child, "Axis Correction");
                child.localRotation = rotation * child.localRotation;
                child.localPosition = rotation * child.localPosition;
            }
        }

        public static bool CorrectPrefabAsset(string path, Quaternion rotation)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.GetComponent<Renderer>() != null || root.transform.childCount == 0)
                {
                    return false;
                }
                RotateChildren(root, rotation);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
