using System;
using UnityEngine;

namespace Seanzo.LevelDesign.Editor
{
    [Serializable]
    public class KitPaletteEntry
    {
        public GameObject prefab;
        public int defaultRotationStep;
        public SlotKind allowedSlot = SlotKind.Either;
        public Vector3Int detectedFootprint = Vector3Int.one;
        // Zero on any axis means "use the detected footprint".
        public Vector3Int footprintOverride;
        // When set, painting resolves the piece from neighbor rules; rule tiles
        // always occupy a single cell.
        public RuleKitTile ruleTile;
        // How the piece was authored, used to orient it onto painted faces:
        // which local axis points out of the paint plane, and which points along
        // the plane's up. Wall-style pieces are PosZ/PosY (identity); a flat
        // floor panel is PosY facing. Content painting ignores these.
        public AuthoredAxis authoredFacing = AuthoredAxis.PosZ;
        public AuthoredAxis authoredUp = AuthoredAxis.PosY;
        // False once the axes were set by hand; re-detection then leaves them alone.
        public bool authoredAxesAuto = true;

        public Vector3Int Footprint =>
            ruleTile != null
                ? Vector3Int.one
                : footprintOverride.x > 0 && footprintOverride.y > 0 && footprintOverride.z > 0
                    ? footprintOverride
                    : detectedFootprint;

        public bool HasOverride =>
            footprintOverride.x > 0 && footprintOverride.y > 0 && footprintOverride.z > 0;
    }
}
