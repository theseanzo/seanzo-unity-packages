using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Seanzo.LevelDesign.Editor;

namespace Seanzo.LevelDesign.Tests
{
    public class GridPaintOperationsTests
    {
        private const string TempFolder = "Assets/_TempPaintTests";

        private GameObject gridObject;
        private GridLevel grid;
        private GameObject cubePrefab;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            if (!AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.CreateFolder("Assets", "_TempPaintTests");
            }
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubePrefab = PrefabUtility.SaveAsPrefabAsset(cube, $"{TempFolder}/PaintCube.prefab");
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
            gridObject = new GameObject("PaintTestGrid");
            grid = gridObject.AddComponent<GridLevel>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gridObject);
        }

        private KitPaletteEntry Entry(Vector3Int footprint)
        {
            return new KitPaletteEntry { prefab = cubePrefab, detectedFootprint = footprint };
        }

        [Test]
        public void Paint_PlacesInstanceParentedAndCentered()
        {
            var cell = new Vector3Int(0, 0, 0);
            Assert.IsTrue(GridPaintOperations.Paint(grid, Entry(Vector3Int.one), cell, 0, 0));

            Assert.IsTrue(grid.TryGetContent(cell, out _, out var slot));
            Assert.IsNotNull(slot.instance);
            Assert.AreEqual(grid.transform, slot.instance.transform.parent);
            Assert.AreEqual(new Vector3(0.5f, 0f, 0.5f), slot.instance.transform.localPosition);
        }

        [Test]
        public void Paint_RotationStep_RotatesFootprintAndInstance()
        {
            var entry = Entry(new Vector3Int(2, 1, 1));
            var anchor = new Vector3Int(0, 0, 0);
            Assert.IsTrue(GridPaintOperations.Paint(grid, entry, anchor, 1, 0));

            Assert.IsTrue(grid.IsContentOccupied(new Vector3Int(0, 0, 1)));
            Assert.IsFalse(grid.IsContentOccupied(new Vector3Int(1, 0, 0)));

            grid.TryGetContent(anchor, out _, out var slot);
            Assert.AreEqual(new Vector3Int(1, 1, 2), slot.footprint);
            Assert.AreEqual(90f, slot.instance.transform.localEulerAngles.y, 0.01f);
        }

        [Test]
        public void Paint_PartialOverlapWithOtherPiece_IsRefused()
        {
            Assert.IsTrue(GridPaintOperations.Paint(grid, Entry(new Vector3Int(2, 1, 2)), Vector3Int.zero, 0, 0));
            Assert.IsFalse(GridPaintOperations.Paint(grid, Entry(new Vector3Int(2, 1, 2)), new Vector3Int(1, 0, 1), 0, 0));
            Assert.AreEqual(1, grid.ContentCount);
        }

        [Test]
        public void Paint_OccupiedSingleCell_ReplacesPiece()
        {
            var cell = new Vector3Int(2, 0, 2);
            GridPaintOperations.Paint(grid, Entry(Vector3Int.one), cell, 0, 0);
            grid.TryGetContent(cell, out _, out var first);
            var firstInstance = first.instance;

            Assert.IsTrue(GridPaintOperations.Paint(grid, Entry(Vector3Int.one), cell, 1, 0));
            Assert.AreEqual(1, grid.ContentCount);
            Assert.IsTrue(firstInstance == null);
            grid.TryGetContent(cell, out _, out var second);
            Assert.AreEqual(1, second.rotationStep);
        }

        [Test]
        public void Paint_SameAnchorMultiCell_Replaces()
        {
            var anchor = new Vector3Int(0, 0, 0);
            var entry = Entry(new Vector3Int(2, 1, 2));
            GridPaintOperations.Paint(grid, entry, anchor, 0, 0);

            Assert.IsTrue(GridPaintOperations.Paint(grid, entry, anchor, 0, 0));
            Assert.AreEqual(1, grid.ContentCount);
        }

        [Test]
        public void Erase_RemovesPieceAndInstanceFromAnyCoveredCell()
        {
            var anchor = new Vector3Int(0, 0, 0);
            GridPaintOperations.Paint(grid, Entry(new Vector3Int(3, 1, 1)), anchor, 0, 0);
            grid.TryGetContent(anchor, out _, out var slot);
            var instance = slot.instance;

            Assert.IsTrue(GridPaintOperations.Erase(grid, new Vector3Int(2, 0, 0)));
            Assert.AreEqual(0, grid.ContentCount);
            Assert.IsTrue(instance == null);
            Assert.IsFalse(GridPaintOperations.Erase(grid, anchor));
        }

        [Test]
        public void PruneMissing_RecoversFromManuallyDeletedInstances()
        {
            GridPaintOperations.Paint(grid, Entry(Vector3Int.one), Vector3Int.zero, 0, 0);
            grid.TryGetContent(Vector3Int.zero, out _, out var slot);
            Object.DestroyImmediate(slot.instance);

            Assert.AreEqual(1, GridPaintOperations.PruneMissing(grid));
            Assert.AreEqual(0, grid.ContentCount);
            Assert.IsTrue(GridPaintOperations.Paint(grid, Entry(Vector3Int.one), Vector3Int.zero, 0, 0));
        }

        [Test]
        public void TryPick_ReturnsPrefabAndRotation()
        {
            var cell = new Vector3Int(1, 0, 1);
            GridPaintOperations.Paint(grid, Entry(Vector3Int.one), cell, 3, 0);

            Assert.IsTrue(GridPaintOperations.TryPick(grid, cell, out var prefab, out var rotation));
            Assert.AreEqual(cubePrefab, prefab);
            Assert.AreEqual(3, rotation);
            Assert.IsFalse(GridPaintOperations.TryPick(grid, new Vector3Int(9, 9, 9), out _, out _));
        }
    }
}
