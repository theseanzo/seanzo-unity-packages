using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Seanzo.LevelDesign.Editor;

namespace Seanzo.LevelDesign.Tests
{
    public class FacePaintOperationsTests
    {
        private const string TempFolder = "Assets/_TempFacePaintTests";

        private GameObject gridObject;
        private GridLevel grid;
        private GameObject wallPrefab;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            if (!AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.CreateFolder("Assets", "_TempFacePaintTests");
            }
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallPrefab = PrefabUtility.SaveAsPrefabAsset(wall, $"{TempFolder}/Wall.prefab");
            Object.DestroyImmediate(wall);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            AssetDatabase.DeleteAsset(TempFolder);
        }

        [SetUp]
        public void SetUp()
        {
            gridObject = new GameObject("FacePaintTestGrid");
            grid = gridObject.AddComponent<GridLevel>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gridObject);
        }

        private KitPaletteEntry WallEntry()
        {
            return new KitPaletteEntry { prefab = wallPrefab, allowedSlot = SlotKind.Face };
        }

        [Test]
        public void PaintFace_PlacesInstanceAtFaceAnchor()
        {
            var cell = new Vector3Int(2, 0, 2);
            Assert.IsTrue(GridPaintOperations.PaintFace(grid, WallEntry(), cell, CellFace.NegZ, 0, 0));

            var slot = grid.GetFace(cell, CellFace.NegZ);
            Assert.IsNotNull(slot);
            Assert.IsNotNull(slot.instance);
            Assert.AreEqual(grid.transform, slot.instance.transform.parent);
            Assert.AreEqual(new Vector3(2.5f, 0f, 2f), slot.instance.transform.localPosition);

            var forward = slot.instance.transform.localRotation * Vector3.forward;
            Assert.Less(Vector3.Distance(forward, new Vector3(0f, 0f, -1f)), 0.001f);
        }

        [Test]
        public void PaintFace_SameFace_Replaces()
        {
            var cell = new Vector3Int(0, 0, 0);
            GridPaintOperations.PaintFace(grid, WallEntry(), cell, CellFace.NegX, 0, 0);
            var first = grid.GetFace(cell, CellFace.NegX).instance;

            Assert.IsTrue(GridPaintOperations.PaintFace(grid, WallEntry(), cell, CellFace.NegX, 2, 0));
            Assert.AreEqual(1, grid.FaceCount);
            Assert.IsTrue(first == null);
            Assert.AreEqual(2, grid.GetFace(cell, CellFace.NegX).rotationStep);
        }

        [Test]
        public void PaintFace_DoesNotTouchContentSlot()
        {
            var cell = new Vector3Int(1, 0, 1);
            grid.SetContent(cell, new ContentSlot { footprint = Vector3Int.one });

            Assert.IsTrue(GridPaintOperations.PaintFace(grid, WallEntry(), cell, CellFace.PosZ, 0, 0));
            Assert.IsTrue(grid.IsContentOccupied(cell));
            Assert.AreEqual(1, grid.FaceCount);
        }

        [Test]
        public void EraseFace_RemovesSlotAndInstance()
        {
            var cell = new Vector3Int(0, 0, 0);
            GridPaintOperations.PaintFace(grid, WallEntry(), cell, CellFace.PosX, 0, 0);
            var instance = grid.GetFace(cell, CellFace.PosX).instance;

            Assert.IsTrue(GridPaintOperations.EraseFace(grid, cell, CellFace.PosX));
            Assert.IsNull(grid.GetFace(cell, CellFace.PosX));
            Assert.IsTrue(instance == null);
            Assert.IsFalse(GridPaintOperations.EraseFace(grid, cell, CellFace.PosX));
        }

        [Test]
        public void PaintFace_OutOfBounds_IsRefused()
        {
            grid.UseBounds = true;
            grid.Bounds = new BoundsInt(Vector3Int.zero, new Vector3Int(2, 2, 2));

            Assert.IsFalse(GridPaintOperations.PaintFace(grid, WallEntry(), new Vector3Int(5, 0, 0), CellFace.NegZ, 0, 0));
            Assert.AreEqual(0, grid.FaceCount);
        }

        [Test]
        public void TryPickFace_ReturnsPrefabAndRotation()
        {
            var cell = new Vector3Int(3, 1, 3);
            GridPaintOperations.PaintFace(grid, WallEntry(), cell, CellFace.NegZ, 3, 0);

            Assert.IsTrue(GridPaintOperations.TryPickFace(grid, cell, CellFace.NegZ, out var prefab, out var rotation));
            Assert.AreEqual(wallPrefab, prefab);
            Assert.AreEqual(3, rotation);
            Assert.IsFalse(GridPaintOperations.TryPickFace(grid, cell, CellFace.PosZ, out _, out _));
        }
    }
}
