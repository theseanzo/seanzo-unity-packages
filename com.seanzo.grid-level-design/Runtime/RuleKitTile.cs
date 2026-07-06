using System.Collections.Generic;
using UnityEngine;

namespace Seanzo.LevelDesign
{
    [CreateAssetMenu(menuName = "Seanzo/Level Design/Rule Kit Tile", fileName = "RuleKitTile")]
    public sealed class RuleKitTile : ScriptableObject
    {
        // Placed when no rule matches.
        public GameObject defaultPrefab;
        [Range(0, 3)] public int defaultRotationStep;
        public List<KitTileRule> rules = new();
    }
}
