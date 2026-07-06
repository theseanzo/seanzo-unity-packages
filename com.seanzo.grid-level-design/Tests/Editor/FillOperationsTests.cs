using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Seanzo.LevelDesign.Editor;

namespace Seanzo.LevelDesign.Tests
{
    public class FillOperationsTests
    {
        private const string TempFolder = "Assets/_TempFillTests";

        private static readonly PaintPlane Ground = new(PlaneOrientation.XZ, 0);

        private GameObject gridObject;
        private GridLevel grid;
        private GameObject prefabA;
        private GameObject prefabB;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            if (!AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.CreateFolder("Assets", "_TempFillTests");
            }
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prefabA = PrefabUtility.SaveAsPrefabAsset(cube, $"{TempFolder}/FillA.prefab");
            prefabB = PrefabUtility.SaveAsPrefabAsset(cube, $"{TempFolder}/FillB.prefab");
            Object.DestroyImmediate(cube);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            AssetDatabase.DeleteAsset(TempFolder);
        }

        [SetUp]
        public void SetUp()
        {
            gridObject = new GameObject("FillTestGrid");
            grid = gridObject.AddComponent<GridLevel>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gridObject);
        }

        private KitPaletteEntry Entry(GameObject prefab, Vector3Int footprint, SlotKind slot = SlotKind.Content)
        {
            return new KitPaletteEntry { prefab = prefab, detectedFootprint = footprint, allowedSlot = slot };
        }

        [Test]
        public void RectCells_CoversInclusiveRect()
        {
            var cells = FillOperations.RectCells(Ground, new Vector3Int(2, 0, 2), new Vector3Int(0, 0, 0)).ToList();
            Assert.AreEqual(9, cells.Count);
            Assert.Contains(new Vector3Int(1, 0, 1), cells);
        }

        [Test]
        public void BoxFill_FillsEveryCellInRect()
        {
            var count = FillOperations.BoxFill(
                grid, Entry(prefabA, Vector3Int.one), Ground,
                new Vector3Int(0, 0, 0), new Vector3Int(2, 0, 2), 0, 0, false);
            Assert.AreEqual(9, count);
            Assert.AreEqual(9, grid.ContentCount);
        }

        [Test]
        public void BoxFill_SkipsOccupiedCells()
        {
            GridPaintOperations.Paint(grid, Entry(prefabB, Vector3Int.one), new Vector3Int(1, 0, 1), 0, 0);
            var count = FillOperations.BoxFill(
                grid, Entry(prefabA, Vector3Int.one), Ground,
                new Vector3Int(0, 0, 0), new Vector3Int(2, 0, 2), 0, 0, false);
            Assert.AreEqual(8, count);
            grid.TryGetContent(new Vector3Int(1, 0, 1), out _, out var kept);
            Assert.AreEqual(prefabB, kept.prefab);
        }

        [Test]
        public void BoxFill_MultiCell_TilesByFootprintStride()
        {
            var count = FillOperations.BoxFill(
                grid, Entry(prefabA, new Vector3Int(2, 1, 2)), Ground,
                new Vector3Int(0, 0, 0), new Vector3Int(4, 0, 4), 0, 0, false);
            Assert.AreEqual(4, count);
            Assert.IsTrue(grid.IsContentOccupied(new Vector3Int(3, 0, 3)));
            Assert.IsFalse(grid.IsContentOccupied(new Vector3Int(4, 0, 4)));
        }

        [Test]
        public void BoxErase_ClearsRect()
        {
            FillOperations.BoxFill(
                grid, Entry(prefabA, Vector3Int.one), Ground,
                new Vector3Int(0, 0, 0), new Vector3Int(3, 0, 3), 0, 0, false);
            var count = FillOperations.BoxErase(grid, Ground, new Vector3Int(1, 0, 1), new Vector3Int(2, 0, 2), false);
            Assert.AreEqual(4, count);
            Assert.AreEqual(12, grid.ContentCount);
        }

        [Test]
        public void FloodFill_FillsBoundedEmptyRegion()
        {
            grid.UseBounds = true;
            grid.Bounds = new BoundsInt(Vector3Int.zero, new Vector3Int(4, 1, 4));

            var count = FillOperations.FloodFill(
                grid, Entry(prefabA, Vector3Int.one), Ground, new Vector3Int(1, 0, 1), 0, 0, false, out var truncated);
            Assert.AreEqual(16, count);
            Assert.IsFalse(truncated);
        }

        [Test]
        public void FloodFill_StopsAtOccupiedCells()
        {
            grid.UseBounds = true;
            grid.Bounds = new BoundsInt(Vector3Int.zero, new Vector3Int(5, 1, 5));
            for (var z = 0; z < 5; z++)
            {
                GridPaintOperations.Paint(grid, Entry(prefabB, Vector3Int.one), new Vector3Int(2, 0, z), 0, 0);
            }

            var count = FillOperations.FloodFill(
                grid, Entry(prefabA, Vector3Int.one), Ground, new Vector3Int(0, 0, 0), 0, 0, false, out _);
            Assert.AreEqual(10, count);
            Assert.IsFalse(grid.TryGetContent(new Vector3Int(3, 0, 0), out _, out _));
        }

        [Test]
        public void FloodFill_SamePrefabRegion_Replaces()
        {
            GridPaintOperations.Paint(grid, Entry(prefabA, Vector3Int.one), new Vector3Int(0, 0, 0), 0, 0);
            GridPaintOperations.Paint(grid, Entry(prefabA, Vector3Int.one), new Vector3Int(1, 0, 0), 0, 0);
            GridPaintOperations.Paint(grid, Entry(prefabB, Vector3Int.one), new Vector3Int(2, 0, 0), 0, 0);

            var count = FillOperations.FloodFill(
                grid, Entry(prefabB, Vector3Int.one), Ground, new Vector3Int(0, 0, 0), 0, 0, false, out _);
            Assert.AreEqual(2, count);

            grid.TryGetContent(new Vector3Int(0, 0, 0), out _, out var replaced);
            Assert.AreEqual(prefabB, replaced.prefab);
            Assert.AreEqual(3, grid.ContentCount);
        }

        [Test]
        public void FloodFill_WithoutBounds_TruncatesAtCap()
        {
            var count = FillOperations.FloodFill(
                grid, Entry(prefabA, Vector3Int.one), Ground, Vector3Int.zero, 0, 0, false, out var truncated, cap: 10);
            Assert.IsTrue(truncated);
            Assert.AreEqual(10, count);
        }

        [Test]
        public void BoxFill_VerticalPlane_PaintsFaces()
        {
            var plane = new PaintPlane(PlaneOrientation.XY, 0);
            var count = FillOperations.BoxFill(
                grid, Entry(prefabA, Vector3Int.one, SlotKind.Face), plane,
                new Vector3Int(0, 0, 0), new Vector3Int(2, 1, 0), 0, 0, false);
            Assert.AreEqual(6, count);
            Assert.AreEqual(6, grid.FaceCount);
            Assert.IsNotNull(grid.GetFace(new Vector3Int(2, 1, 0), CellFace.NegZ));
            Assert.AreEqual(0, grid.ContentCount);
        }

        [Test]
        public void FloodFill_VerticalPlane_FillsFaceRegionWithinBounds()
        {
            grid.UseBounds = true;
            grid.Bounds = new BoundsInt(Vector3Int.zero, new Vector3Int(3, 2, 3));

            var plane = new PaintPlane(PlaneOrientation.XY, 0);
            var count = FillOperations.FloodFill(
                grid, Entry(prefabA, Vector3Int.one, SlotKind.Face), plane, new Vector3Int(0, 0, 0), 0, 0, false, out var truncated);
            Assert.AreEqual(6, count);
            Assert.IsFalse(truncated);
            Assert.AreEqual(6, grid.FaceCount);
        }

        [Test]
        public void StrideAnchors_AlignToRegionMin()
        {
            var region = FillOperations.RectCells(Ground, new Vector3Int(1, 0, 1), new Vector3Int(4, 0, 4)).ToList();
            var anchors = FillOperations.StrideAnchors(Ground, region, new Vector3Int(2, 1, 2));
            Assert.AreEqual(4, anchors.Count);
            Assert.Contains(new Vector3Int(1, 0, 1), anchors);
            Assert.Contains(new Vector3Int(3, 0, 3), anchors);
        }
    }
}
