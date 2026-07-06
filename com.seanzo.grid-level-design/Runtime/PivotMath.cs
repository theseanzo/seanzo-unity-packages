using UnityEngine;

namespace Seanzo.LevelDesign
{
    public static class PivotMath
    {
        // Combined renderer bounds in root-local space, independent of the root's
        // world placement (equivalent to baking the object at identity).
        public static bool TryCombinedBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            if (root == null)
            {
                return false;
            }
            var found = false;
            var toLocal = root.transform.worldToLocalMatrix;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                var localBounds = renderer.localBounds;
                var toRoot = toLocal * renderer.transform.localToWorldMatrix;
                for (var i = 0; i < 8; i++)
                {
                    var corner = localBounds.min + Vector3.Scale(
                        localBounds.size, new Vector3(i & 1, (i >> 1) & 1, (i >> 2) & 1));
                    var point = toRoot.MultiplyPoint3x4(corner);
                    if (!found)
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        found = true;
                    }
                    else
                    {
                        bounds.Encapsulate(point);
                    }
                }
            }
            return found;
        }

        // Steps per axis: 0 = min, 1 = center, 2 = max.
        public static Vector3 SnapPoint(Bounds bounds, int xStep, int yStep, int zStep)
        {
            return new Vector3(
                AxisValue(bounds.min.x, bounds.max.x, xStep),
                AxisValue(bounds.min.y, bounds.max.y, yStep),
                AxisValue(bounds.min.z, bounds.max.z, zStep));
        }

        private static float AxisValue(float min, float max, int step)
        {
            return step switch
            {
                0 => min,
                1 => (min + max) * 0.5f,
                _ => max
            };
        }
    }
}
