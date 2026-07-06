using System;
using UnityEngine;

namespace Seanzo.LevelDesign
{
    [Serializable]
    public class KitTileRule
    {
        // Eight in-plane neighbors in RuleMatcher.Offsets order.
        public NeighborRule[] neighbors = new NeighborRule[8];
        public GameObject prefab;
        [Range(0, 3)] public int rotationStep;
        // When set, the matcher also tries the mask at 90/180/270 degrees and
        // adds the matched rotation to the output rotation step.
        public bool matchRotated = true;

        public NeighborRule NeighborAt(int index)
        {
            return neighbors != null && index >= 0 && index < neighbors.Length
                ? neighbors[index]
                : NeighborRule.Any;
        }
    }
}
