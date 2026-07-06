using System.Collections.Generic;
using UnityEngine;

namespace Seanzo.LevelDesign.Editor
{
    [CreateAssetMenu(menuName = "Seanzo/Level Design/Kit Palette", fileName = "KitPalette")]
    public sealed class KitPalette : ScriptableObject
    {
        public Vector3 moduleSize = Vector3.one;
        public List<KitPaletteEntry> entries = new();
    }
}
