using UnityEngine;

namespace Seanzo.LevelDesign.Editor
{
    public static class FootprintDetector
    {
        // Fraction of a cell tolerated past a module boundary before the next cell is
        // counted. Covers kits that oversize pieces slightly to hide seams.
        private const float Epsilon = 0.05f;

        // A piece reads as flat when its thinnest axis is at most this fraction of
        // the next-thinnest; anything chunkier keeps the wall-convention default.
        private const float FlatnessRatio = 0.5f;

        public static Vector3Int Detect(GameObject prefab, Vector3 moduleSize)
        {
            if (!TryMeasureLocalBounds(prefab, out var bounds))
            {
                return Vector3Int.one;
            }
            return new Vector3Int(
                CellCount(bounds.size.x, moduleSize.x),
                CellCount(bounds.size.y, moduleSize.y),
                CellCount(bounds.size.z, moduleSize.z));
        }

        // Authored basis from the shape: a piece flat along one axis faces along
        // that axis (a floor panel is thin in Y, a wall panel thin in Z).
        public static void DetectAuthoredAxes(GameObject prefab, out AuthoredAxis facing, out AuthoredAxis up)
        {
            facing = AuthoredAxis.PosZ;
            up = AuthoredAxis.PosY;
            if (!TryMeasureLocalBounds(prefab, out var bounds))
            {
                return;
            }

            var size = bounds.size;
            var thinAxis = size.x <= size.y
                ? size.x <= size.z ? 0 : 2
                : size.y <= size.z ? 1 : 2;
            var secondThinnest = Mathf.Min(
                thinAxis == 0 ? size.y : size.x,
                thinAxis == 2 ? size.y : size.z);
            if (size[thinAxis] > secondThinnest * FlatnessRatio)
            {
                return;
            }

            facing = thinAxis switch
            {
                0 => AuthoredAxis.PosX,
                1 => AuthoredAxis.PosY,
                _ => AuthoredAxis.PosZ
            };
            up = thinAxis == 1 ? AuthoredAxis.PosZ : AuthoredAxis.PosY;
        }

        // Bounds of the prefab's renderers in prefab-local space, measured on a
        // temporary unrotated instance.
        public static bool TryMeasureLocalBounds(GameObject prefab, out Bounds bounds)
        {
            bounds = default;
            if (prefab == null)
            {
                return false;
            }

            var temp = Object.Instantiate(prefab);
            temp.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                temp.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var renderers = temp.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0)
                {
                    return false;
                }

                bounds = renderers[0].bounds;
                for (var i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
                return true;
            }
            finally
            {
                Object.DestroyImmediate(temp);
            }
        }

        private static int CellCount(float size, float module)
        {
            if (module <= 0f)
            {
                return 1;
            }
            return Mathf.Max(1, Mathf.CeilToInt(size / module - Epsilon));
        }
    }
}
