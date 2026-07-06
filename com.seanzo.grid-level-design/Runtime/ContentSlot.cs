using System;
using UnityEngine;

namespace Seanzo.LevelDesign
{
    [Serializable]
    public class ContentSlot : CellSlot
    {
        public Vector3Int footprint = Vector3Int.one;
    }
}
