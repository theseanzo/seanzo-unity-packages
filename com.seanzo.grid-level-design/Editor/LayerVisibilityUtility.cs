using UnityEditor;
using UnityEngine;

namespace Seanzo.LevelDesign.Editor
{
    public static class LayerVisibilityUtility
    {
        public static void Apply(GridLevel grid)
        {
            if (grid == null)
            {
                return;
            }
            foreach (var (_, slot) in grid.ContentCells)
            {
                ApplyToInstance(grid, slot.layerId, slot.instance);
            }
            foreach (var (_, _, slot) in grid.FaceCells)
            {
                ApplyToInstance(grid, slot.layerId, slot.instance);
            }
        }

        public static void ApplyToInstance(GridLevel grid, int layerId, GameObject instance)
        {
            if (instance == null)
            {
                return;
            }
            var layer = grid.FindLayer(layerId);
            var visible = layer == null || layer.visible;
            if (visible)
            {
                SceneVisibilityManager.instance.Show(instance, true);
            }
            else
            {
                SceneVisibilityManager.instance.Hide(instance, true);
            }
        }
    }
}
