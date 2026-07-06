using System;
using UnityEngine;

namespace Seanzo.LevelDesign
{
    [Serializable]
    public class CellSlot
    {
        public GameObject prefab;
        public int rotationStep;
        public int layerId;
        public GameObject instance;
        // Set when the slot was painted from a rule-tile entry; prefab then holds
        // the resolved variant and the slot re-resolves when neighbors change.
        public RuleKitTile ruleTile;
        // Palette prefab the slot was painted with. Re-resolution falls back to
        // it when no rule matches, matching what a fresh paint would place.
        public GameObject sourcePrefab;
        // Authored basis of the painted entry, kept so rule re-resolution can
        // re-place the instance with the same orientation.
        public AuthoredAxis authoredFacing = AuthoredAxis.PosZ;
        public AuthoredAxis authoredUp = AuthoredAxis.PosY;
    }
}
