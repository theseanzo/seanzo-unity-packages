using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Seanzo.LevelDesign.Tests
{
    public class RuleMatcherTests
    {
        private readonly List<Object> created = new();
        private RuleKitTile tile;
        private GameObject defaultPrefab;
        private GameObject variantPrefab;
        private GameObject otherPrefab;

        [SetUp]
        public void SetUp()
        {
            tile = ScriptableObject.CreateInstance<RuleKitTile>();
            defaultPrefab = new GameObject("Default");
            variantPrefab = new GameObject("Variant");
            otherPrefab = new GameObject("Other");
            created.Add(tile);
            created.Add(defaultPrefab);
            created.Add(variantPrefab);
            created.Add(otherPrefab);
            tile.defaultPrefab = defaultPrefab;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in created)
            {
                Object.DestroyImmediate(obj);
            }
            created.Clear();
        }

        private static System.Func<int, int, bool> Occupancy(params Vector2Int[] filled)
        {
            var set = new HashSet<Vector2Int>(filled);
            return (a, b) => set.Contains(new Vector2Int(a, b));
        }

        private static KitTileRule Rule(
            GameObject prefab, bool matchRotated, params (Vector2Int offset, NeighborRule condition)[] conditions)
        {
            var rule = new KitTileRule { prefab = prefab, matchRotated = matchRotated };
            foreach (var (offset, condition) in conditions)
            {
                var index = System.Array.IndexOf(RuleMatcher.Offsets, offset);
                Assert.GreaterOrEqual(index, 0, $"Offset {offset} is not a neighbor offset.");
                rule.neighbors[index] = condition;
            }
            return rule;
        }

        [Test]
        public void Resolve_NoRules_FallsBackToDefault()
        {
            tile.defaultRotationStep = 2;

            var matched = RuleMatcher.Resolve(tile, Occupancy(), out var prefab, out var rotation);

            Assert.IsFalse(matched);
            Assert.AreEqual(defaultPrefab, prefab);
            Assert.AreEqual(2, rotation);
        }

        [Test]
        public void Resolve_FilledAndEmptyConditions_MatchExactly()
        {
            tile.rules.Add(Rule(variantPrefab, false,
                (new Vector2Int(1, 0), NeighborRule.Filled),
                (new Vector2Int(-1, 0), NeighborRule.Empty)));

            Assert.IsTrue(RuleMatcher.Resolve(tile, Occupancy(new Vector2Int(1, 0)), out var prefab, out var rotation));
            Assert.AreEqual(variantPrefab, prefab);
            Assert.AreEqual(0, rotation);

            Assert.IsFalse(RuleMatcher.Resolve(
                tile, Occupancy(new Vector2Int(1, 0), new Vector2Int(-1, 0)), out prefab, out _));
            Assert.AreEqual(defaultPrefab, prefab);
        }

        [Test]
        public void Resolve_RotatedReuse_MatchesAndAddsRotation()
        {
            tile.rules.Add(Rule(variantPrefab, true,
                (new Vector2Int(1, 0), NeighborRule.Filled),
                (new Vector2Int(-1, 0), NeighborRule.Empty)));

            // Filled at (-1, 0) only: the mask matches rotated 180 degrees.
            Assert.IsTrue(RuleMatcher.Resolve(tile, Occupancy(new Vector2Int(-1, 0)), out var prefab, out var rotation));
            Assert.AreEqual(variantPrefab, prefab);
            Assert.AreEqual(2, rotation);

            // Filled at (0, -1) only: (1, 0) rotates there at step 1.
            Assert.IsTrue(RuleMatcher.Resolve(tile, Occupancy(new Vector2Int(0, -1)), out _, out rotation));
            Assert.AreEqual(1, rotation);
        }

        [Test]
        public void Resolve_MatchRotatedDisabled_DoesNotRotate()
        {
            tile.rules.Add(Rule(variantPrefab, false,
                (new Vector2Int(1, 0), NeighborRule.Filled),
                (new Vector2Int(-1, 0), NeighborRule.Empty)));

            Assert.IsFalse(RuleMatcher.Resolve(tile, Occupancy(new Vector2Int(-1, 0)), out var prefab, out _));
            Assert.AreEqual(defaultPrefab, prefab);
        }

        [Test]
        public void Resolve_FirstMatchingRuleWins()
        {
            tile.rules.Add(Rule(variantPrefab, false, (new Vector2Int(1, 0), NeighborRule.Filled)));
            tile.rules.Add(Rule(otherPrefab, false, (new Vector2Int(1, 0), NeighborRule.Filled)));

            Assert.IsTrue(RuleMatcher.Resolve(tile, Occupancy(new Vector2Int(1, 0)), out var prefab, out _));
            Assert.AreEqual(variantPrefab, prefab);
        }

        [Test]
        public void Resolve_NullPrefabRule_IsSkipped()
        {
            tile.rules.Add(Rule(null, false, (new Vector2Int(1, 0), NeighborRule.Filled)));
            tile.rules.Add(Rule(variantPrefab, false, (new Vector2Int(1, 0), NeighborRule.Filled)));

            Assert.IsTrue(RuleMatcher.Resolve(tile, Occupancy(new Vector2Int(1, 0)), out var prefab, out _));
            Assert.AreEqual(variantPrefab, prefab);
        }

        [Test]
        public void Resolve_RuleRotationComposesWithMatchedRotation()
        {
            var rule = Rule(variantPrefab, true,
                (new Vector2Int(1, 0), NeighborRule.Filled),
                (new Vector2Int(-1, 0), NeighborRule.Empty));
            rule.rotationStep = 3;
            tile.rules.Add(rule);

            // Matches rotated 180 degrees: (3 + 2) % 4 == 1.
            Assert.IsTrue(RuleMatcher.Resolve(tile, Occupancy(new Vector2Int(-1, 0)), out _, out var rotation));
            Assert.AreEqual(1, rotation);
        }

        [Test]
        public void Resolve_AnyCondition_IgnoresOccupancy()
        {
            tile.rules.Add(Rule(variantPrefab, false));

            Assert.IsTrue(RuleMatcher.Resolve(tile, Occupancy(new Vector2Int(1, 1)), out var prefab, out _));
            Assert.AreEqual(variantPrefab, prefab);
        }

        [Test]
        public void Rotate_MapsOffsetsClockwise()
        {
            Assert.AreEqual(new Vector2Int(0, -1), RuleMatcher.Rotate(new Vector2Int(1, 0), 1));
            Assert.AreEqual(new Vector2Int(1, 0), RuleMatcher.Rotate(new Vector2Int(0, 1), 1));
            Assert.AreEqual(new Vector2Int(-1, -1), RuleMatcher.Rotate(new Vector2Int(1, -1), 1));
            Assert.AreEqual(new Vector2Int(-1, 0), RuleMatcher.Rotate(new Vector2Int(1, 0), 2));
            Assert.AreEqual(new Vector2Int(1, 0), RuleMatcher.Rotate(new Vector2Int(1, 0), 4));
        }
    }
}
