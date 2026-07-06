using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Seanzo.LevelDesign.Editor;

namespace Seanzo.LevelDesign.Tests
{
    public class RuleResolutionTests
    {
        private const string TempFolder = "Assets/_TempRuleTests";

        private GameObject gridObject;
        private GridLevel grid;
        private GameObject soloPrefab;
        private GameObject endPrefab;
        private GameObject straightPrefab;
        private GameObject plainPrefab;
        private RuleKitTile tile;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            if (!AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.CreateFolder("Assets", "_TempRuleTests");
            }
            soloPrefab = SavePrefab("RuleSolo");
            endPrefab = SavePrefab("RuleEnd");
            straightPrefab = SavePrefab("RuleStraight");
            plainPrefab = SavePrefab("RulePlain");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            AssetDatabase.DeleteAsset(TempFolder);
        }

        [SetUp]
        public void SetUp()
        {
            gridObject = new GameObject("RuleTestGrid");
            grid = gridObject.AddComponent<GridLevel>();
            tile = BuildRunTile();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gridObject);
            Object.DestroyImmediate(tile);
        }

        private static GameObject SavePrefab(string name)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var prefab = PrefabUtility.SaveAsPrefabAsset(cube, $"{TempFolder}/{name}.prefab");
            Object.DestroyImmediate(cube);
            return prefab;
        }

        // Run tile: straight when both in-line neighbors are filled, end cap when only
        // the +a neighbor is filled (rotation-aware), solo otherwise.
        private RuleKitTile BuildRunTile()
        {
            var runTile = ScriptableObject.CreateInstance<RuleKitTile>();
            runTile.defaultPrefab = soloPrefab;

            var straight = new KitTileRule { prefab = straightPrefab, matchRotated = true };
            straight.neighbors[IndexOf(new Vector2Int(-1, 0))] = NeighborRule.Filled;
            straight.neighbors[IndexOf(new Vector2Int(1, 0))] = NeighborRule.Filled;
            runTile.rules.Add(straight);

            var end = new KitTileRule { prefab = endPrefab, matchRotated = true };
            end.neighbors[IndexOf(new Vector2Int(1, 0))] = NeighborRule.Filled;
            end.neighbors[IndexOf(new Vector2Int(-1, 0))] = NeighborRule.Empty;
            runTile.rules.Add(end);

            return runTile;
        }

        private static int IndexOf(Vector2Int offset)
        {
            return System.Array.IndexOf(RuleMatcher.Offsets, offset);
        }

        private KitPaletteEntry RuleEntry()
        {
            return new KitPaletteEntry { ruleTile = tile };
        }

        private KitPaletteEntry PlainEntry(Vector3Int footprint)
        {
            return new KitPaletteEntry { prefab = plainPrefab, detectedFootprint = footprint };
        }

        private ContentSlot Slot(Vector3Int cell)
        {
            Assert.IsTrue(grid.TryGetContent(cell, out var anchor, out var slot));
            Assert.AreEqual(cell, anchor);
            return slot;
        }

        [Test]
        public void RuleEntry_FootprintIsAlwaysOne()
        {
            var entry = RuleEntry();
            entry.detectedFootprint = new Vector3Int(2, 1, 2);
            entry.footprintOverride = new Vector3Int(3, 1, 3);

            Assert.AreEqual(Vector3Int.one, entry.Footprint);
        }

        [Test]
        public void Paint_Alone_UsesDefaultPrefab()
        {
            Assert.IsTrue(GridPaintOperations.Paint(grid, RuleEntry(), Vector3Int.zero, 0, 0));

            var slot = Slot(Vector3Int.zero);
            Assert.AreEqual(soloPrefab, slot.prefab);
            Assert.AreEqual(tile, slot.ruleTile);
        }

        [Test]
        public void Paint_SecondCell_ResolvesBothAsEnds()
        {
            GridPaintOperations.Paint(grid, RuleEntry(), Vector3Int.zero, 0, 0);
            GridPaintOperations.Paint(grid, RuleEntry(), new Vector3Int(1, 0, 0), 0, 0);

            var first = Slot(Vector3Int.zero);
            Assert.AreEqual(endPrefab, first.prefab);
            Assert.AreEqual(0, first.rotationStep);

            var second = Slot(new Vector3Int(1, 0, 0));
            Assert.AreEqual(endPrefab, second.prefab);
            Assert.AreEqual(2, second.rotationStep);
        }

        [Test]
        public void Paint_ThirdCell_MakesMiddleStraight()
        {
            GridPaintOperations.Paint(grid, RuleEntry(), Vector3Int.zero, 0, 0);
            GridPaintOperations.Paint(grid, RuleEntry(), new Vector3Int(1, 0, 0), 0, 0);
            GridPaintOperations.Paint(grid, RuleEntry(), new Vector3Int(2, 0, 0), 0, 0);

            Assert.AreEqual(endPrefab, Slot(Vector3Int.zero).prefab);
            Assert.AreEqual(straightPrefab, Slot(new Vector3Int(1, 0, 0)).prefab);
            Assert.AreEqual(endPrefab, Slot(new Vector3Int(2, 0, 0)).prefab);
        }

        [Test]
        public void Paint_ReResolution_ReplacesInstance()
        {
            GridPaintOperations.Paint(grid, RuleEntry(), Vector3Int.zero, 0, 0);
            var soloInstance = Slot(Vector3Int.zero).instance;

            GridPaintOperations.Paint(grid, RuleEntry(), new Vector3Int(1, 0, 0), 0, 0);

            var slot = Slot(Vector3Int.zero);
            Assert.IsTrue(soloInstance == null);
            Assert.IsNotNull(slot.instance);
            Assert.AreEqual(grid.transform, slot.instance.transform.parent);
        }

        [Test]
        public void Erase_ReResolvesRemainingNeighbors()
        {
            GridPaintOperations.Paint(grid, RuleEntry(), Vector3Int.zero, 0, 0);
            GridPaintOperations.Paint(grid, RuleEntry(), new Vector3Int(1, 0, 0), 0, 0);
            GridPaintOperations.Paint(grid, RuleEntry(), new Vector3Int(2, 0, 0), 0, 0);

            Assert.IsTrue(GridPaintOperations.Erase(grid, new Vector3Int(2, 0, 0)));

            var middle = Slot(new Vector3Int(1, 0, 0));
            Assert.AreEqual(endPrefab, middle.prefab);
            Assert.AreEqual(2, middle.rotationStep);
        }

        [Test]
        public void PlainNeighbor_CountsAsFilled()
        {
            GridPaintOperations.Paint(grid, RuleEntry(), Vector3Int.zero, 0, 0);
            GridPaintOperations.Paint(grid, PlainEntry(Vector3Int.one), new Vector3Int(1, 0, 0), 0, 0);

            var slot = Slot(Vector3Int.zero);
            Assert.AreEqual(endPrefab, slot.prefab);
            Assert.AreEqual(0, slot.rotationStep);
        }

        [Test]
        public void MultiCellCoveredCell_CountsAsFilled()
        {
            // 2x1x1 piece anchored at (1,0,0) covers (2,0,0); the rule cell at (3,0,0)
            // sees its -a neighbor filled through the covered cell.
            GridPaintOperations.Paint(grid, PlainEntry(new Vector3Int(2, 1, 1)), new Vector3Int(1, 0, 0), 0, 0);
            GridPaintOperations.Paint(grid, RuleEntry(), new Vector3Int(3, 0, 0), 0, 0);

            var slot = Slot(new Vector3Int(3, 0, 0));
            Assert.AreEqual(endPrefab, slot.prefab);
            Assert.AreEqual(2, slot.rotationStep);
        }

        [Test]
        public void PaintFace_RunResolvesAlongWall()
        {
            var entry = RuleEntry();
            GridPaintOperations.PaintFace(grid, entry, Vector3Int.zero, CellFace.NegZ, 0, 0);
            GridPaintOperations.PaintFace(grid, entry, new Vector3Int(1, 0, 0), CellFace.NegZ, 0, 0);

            var first = grid.GetFace(Vector3Int.zero, CellFace.NegZ);
            Assert.AreEqual(endPrefab, first.prefab);
            Assert.AreEqual(0, first.rotationStep);

            var second = grid.GetFace(new Vector3Int(1, 0, 0), CellFace.NegZ);
            Assert.AreEqual(endPrefab, second.prefab);
            Assert.AreEqual(2, second.rotationStep);
        }

        [Test]
        public void EraseFace_ReResolvesRun()
        {
            var entry = RuleEntry();
            GridPaintOperations.PaintFace(grid, entry, Vector3Int.zero, CellFace.NegZ, 0, 0);
            GridPaintOperations.PaintFace(grid, entry, new Vector3Int(1, 0, 0), CellFace.NegZ, 0, 0);
            GridPaintOperations.PaintFace(grid, entry, new Vector3Int(2, 0, 0), CellFace.NegZ, 0, 0);

            Assert.AreEqual(straightPrefab, grid.GetFace(new Vector3Int(1, 0, 0), CellFace.NegZ).prefab);

            Assert.IsTrue(GridPaintOperations.EraseFace(grid, new Vector3Int(2, 0, 0), CellFace.NegZ));

            var middle = grid.GetFace(new Vector3Int(1, 0, 0), CellFace.NegZ);
            Assert.AreEqual(endPrefab, middle.prefab);
            Assert.AreEqual(2, middle.rotationStep);
        }

        [Test]
        public void ReResolve_NoMatchNoDefault_FallsBackToSourcePrefab()
        {
            var bareTile = ScriptableObject.CreateInstance<RuleKitTile>();
            var end = new KitTileRule { prefab = endPrefab, matchRotated = true };
            end.neighbors[IndexOf(new Vector2Int(1, 0))] = NeighborRule.Filled;
            bareTile.rules.Add(end);
            try
            {
                var entry = new KitPaletteEntry { prefab = plainPrefab, ruleTile = bareTile };
                GridPaintOperations.Paint(grid, entry, Vector3Int.zero, 0, 0);
                Assert.AreEqual(plainPrefab, Slot(Vector3Int.zero).prefab);

                GridPaintOperations.Paint(grid, entry, new Vector3Int(1, 0, 0), 0, 0);
                Assert.AreEqual(endPrefab, Slot(Vector3Int.zero).prefab);

                // Erasing the neighbor re-resolves to no match; with no default
                // prefab the slot falls back to what a fresh paint would place.
                GridPaintOperations.Erase(grid, new Vector3Int(1, 0, 0));
                var slot = Slot(Vector3Int.zero);
                Assert.AreEqual(plainPrefab, slot.prefab);
                Assert.IsNotNull(slot.instance);
            }
            finally
            {
                Object.DestroyImmediate(bareTile);
            }
        }

        [Test]
        public void Erase_RuleCell_DoesNotDisturbFarCells()
        {
            GridPaintOperations.Paint(grid, RuleEntry(), Vector3Int.zero, 0, 0);
            GridPaintOperations.Paint(grid, RuleEntry(), new Vector3Int(1, 0, 0), 0, 0);
            GridPaintOperations.Paint(grid, RuleEntry(), new Vector3Int(4, 0, 0), 0, 0);

            GridPaintOperations.Erase(grid, new Vector3Int(1, 0, 0));

            Assert.AreEqual(soloPrefab, Slot(Vector3Int.zero).prefab);
            Assert.AreEqual(soloPrefab, Slot(new Vector3Int(4, 0, 0)).prefab);
        }
    }
}
