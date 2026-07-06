using System;
using UnityEngine;

namespace Seanzo.LevelDesign
{
    public static class RuleMatcher
    {
        // In-plane neighbor offsets (a, b) in 3x3 reading order: top row (b = +1)
        // left to right, middle row, bottom row. The center cell is not listed.
        public static readonly Vector2Int[] Offsets =
        {
            new(-1, 1), new(0, 1), new(1, 1),
            new(-1, 0), new(1, 0),
            new(-1, -1), new(0, -1), new(1, -1)
        };

        // Returns true when a rule matched. On no match the outputs fall back to
        // the tile's default prefab and rotation (prefab may be null).
        public static bool Resolve(
            RuleKitTile tile, Func<int, int, bool> occupied, out GameObject prefab, out int rotationStep)
        {
            if (tile != null && tile.rules != null)
            {
                foreach (var rule in tile.rules)
                {
                    if (rule?.prefab == null)
                    {
                        continue;
                    }
                    var rotations = rule.matchRotated ? 4 : 1;
                    for (var r = 0; r < rotations; r++)
                    {
                        if (MatchesAt(rule, occupied, r))
                        {
                            prefab = rule.prefab;
                            rotationStep = (((rule.rotationStep + r) % 4) + 4) % 4;
                            return true;
                        }
                    }
                }
            }
            prefab = tile != null ? tile.defaultPrefab : null;
            rotationStep = tile != null ? ((tile.defaultRotationStep % 4) + 4) % 4 : 0;
            return false;
        }

        // +90 degrees of yaw maps in-plane offset (a, b) to (b, -a).
        public static Vector2Int Rotate(Vector2Int offset, int rotationStep)
        {
            var step = ((rotationStep % 4) + 4) % 4;
            for (var i = 0; i < step; i++)
            {
                offset = new Vector2Int(offset.y, -offset.x);
            }
            return offset;
        }

        private static bool MatchesAt(KitTileRule rule, Func<int, int, bool> occupied, int rotationStep)
        {
            for (var i = 0; i < Offsets.Length; i++)
            {
                var condition = rule.NeighborAt(i);
                if (condition == NeighborRule.Any)
                {
                    continue;
                }
                var offset = Rotate(Offsets[i], rotationStep);
                var filled = occupied(offset.x, offset.y);
                if ((condition == NeighborRule.Filled) != filled)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
