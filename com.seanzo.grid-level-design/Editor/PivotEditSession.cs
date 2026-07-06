using UnityEngine;

namespace Seanzo.LevelDesign.Editor
{
    // Pending pivot point (prefab-root local) for the open prefab stage. The state
    // resets whenever a different prefab asset is queried.
    public static class PivotEditSession
    {
        private static string stagePath;
        private static Vector3 pivot;
        private static Vector3? previewModule;

        public static PivotPreviewMode PreviewMode = PivotPreviewMode.Floor;

        // Module size for the placement frame. Defaults to the active palette's
        // module size until overridden.
        public static Vector3 PreviewModuleSize
        {
            get => previewModule
                ?? (PaintSession.Palette != null ? PaintSession.Palette.moduleSize : Vector3.one);
            set => previewModule = Vector3.Max(value, new Vector3(0.001f, 0.001f, 0.001f));
        }

        public static Vector3 GetPivot(string assetPath)
        {
            if (assetPath != stagePath)
            {
                stagePath = assetPath;
                pivot = Vector3.zero;
            }
            return pivot;
        }

        public static void SetPivot(string assetPath, Vector3 value)
        {
            stagePath = assetPath;
            pivot = value;
        }

        public static void Reset(string assetPath)
        {
            SetPivot(assetPath, Vector3.zero);
        }
    }
}
