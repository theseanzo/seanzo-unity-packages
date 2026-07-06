using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Seanzo.LevelDesign.Tests
{
    public class GridLevelTests
    {
        private GameObject gridObject;
        private GridLevel grid;

        [SetUp]
        public void SetUp()
        {
            gridObject = new GameObject("GridLevelTest");
            grid = gridObject.AddComponent<GridLevel>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gridObject);
        }

        private static ContentSlot Content(Vector3Int footprint, int layerId = 0)
        {
            return new ContentSlot { footprint = footprint, layerId = layerId };
        }

        [Test]
        public void SetContent_SingleCell_IsOccupiedAndResolvable()
        {
            var cell = new Vector3Int(2, 0, 3);
            Assert.IsTrue(grid.SetContent(cell, Content(Vector3Int.one)));
            Assert.IsTrue(grid.IsContentOccupied(cell));
            Assert.IsTrue(grid.TryGetContent(cell, out var anchor, out var slot));
            Assert.AreEqual(cell, anchor);
            Assert.IsNotNull(slot);
            Assert.AreEqual(1, grid.ContentCount);
        }

        [Test]
        public void SetContent_MultiCell_CoversFootprintAndResolvesToAnchor()
        {
            var anchor = new Vector3Int(0, 0, 0);
            var footprint = new Vector3Int(2, 1, 3);
            Assert.IsTrue(grid.SetContent(anchor, Content(footprint)));

            foreach (var covered in GridMath.FootprintCells(anchor, footprint))
            {
                Assert.IsTrue(grid.IsContentOccupied(covered), $"cell {covered} should be occupied");
                Assert.IsTrue(grid.TryGetContentAnchor(covered, out var resolved));
                Assert.AreEqual(anchor, resolved);
            }
            Assert.AreEqual(1, grid.ContentCount);
        }

        [Test]
        public void SetContent_OverlappingFootprint_IsRefused()
        {
            Assert.IsTrue(grid.SetContent(Vector3Int.zero, Content(new Vector3Int(2, 1, 2))));
            Assert.IsFalse(grid.SetContent(new Vector3Int(1, 0, 1), Content(Vector3Int.one)));
            Assert.IsFalse(grid.SetContent(new Vector3Int(-1, 0, -1), Content(new Vector3Int(2, 1, 2))));
            Assert.AreEqual(1, grid.ContentCount);
        }

        [Test]
        public void SetContent_InvalidFootprint_IsRefused()
        {
            Assert.IsFalse(grid.SetContent(Vector3Int.zero, Content(new Vector3Int(0, 1, 1))));
            Assert.IsFalse(grid.SetContent(Vector3Int.zero, Content(new Vector3Int(1, -1, 1))));
        }

        [Test]
        public void ClearContent_FromCoveredCell_RemovesWholePiece()
        {
            var anchor = new Vector3Int(0, 0, 0);
            var footprint = new Vector3Int(3, 1, 2);
            grid.SetContent(anchor, Content(footprint));

            var coveredCell = new Vector3Int(2, 0, 1);
            Assert.IsTrue(grid.ClearContent(coveredCell, out var resolvedAnchor, out var removed));
            Assert.AreEqual(anchor, resolvedAnchor);
            Assert.IsNotNull(removed);

            foreach (var cell in GridMath.FootprintCells(anchor, footprint))
            {
                Assert.IsFalse(grid.IsContentOccupied(cell));
            }
            Assert.AreEqual(0, grid.ContentCount);
        }

        [Test]
        public void ClearContent_EmptyCell_ReturnsFalse()
        {
            Assert.IsFalse(grid.ClearContent(new Vector3Int(5, 5, 5), out _, out _));
        }

        [Test]
        public void SetContent_OutOfBounds_IsRefusedWhenBoundsEnabled()
        {
            grid.UseBounds = true;
            grid.Bounds = new BoundsInt(Vector3Int.zero, new Vector3Int(4, 4, 4));

            Assert.IsTrue(grid.SetContent(new Vector3Int(3, 0, 3), Content(Vector3Int.one)));
            Assert.IsFalse(grid.SetContent(new Vector3Int(4, 0, 0), Content(Vector3Int.one)));
            Assert.IsFalse(grid.SetContent(new Vector3Int(3, 0, 0), Content(new Vector3Int(2, 1, 1))));

            grid.UseBounds = false;
            Assert.IsTrue(grid.SetContent(new Vector3Int(10, 0, 10), Content(Vector3Int.one)));
        }

        [Test]
        public void Faces_SetGetClear_IndependentOfContent()
        {
            var cell = new Vector3Int(1, 0, 1);
            grid.SetContent(cell, Content(Vector3Int.one));

            Assert.IsTrue(grid.SetFace(cell, CellFace.PosX, new CellSlot()));
            Assert.IsTrue(grid.SetFace(cell, CellFace.NegZ, new CellSlot()));
            Assert.IsNotNull(grid.GetFace(cell, CellFace.PosX));
            Assert.IsNull(grid.GetFace(cell, CellFace.PosZ));
            Assert.AreEqual(2, grid.FaceCount);

            Assert.IsTrue(grid.ClearFace(cell, CellFace.PosX, out var removed));
            Assert.IsNotNull(removed);
            Assert.IsNull(grid.GetFace(cell, CellFace.PosX));
            Assert.AreEqual(1, grid.FaceCount);

            Assert.IsTrue(grid.IsContentOccupied(cell));
            Assert.IsFalse(grid.ClearFace(cell, CellFace.PosX, out _));
        }

        [Test]
        public void SerializationRoundTrip_PreservesCellMap()
        {
            var anchor = new Vector3Int(-2, 1, 0);
            var footprint = new Vector3Int(2, 1, 2);
            grid.SetContent(anchor, Content(footprint, layerId: 0));
            grid.SetContent(new Vector3Int(5, 0, 5), Content(Vector3Int.one));
            grid.SetFace(new Vector3Int(0, 0, 0), CellFace.PosY, new CellSlot { rotationStep = 2 });

            var copyObject = Object.Instantiate(gridObject);
            try
            {
                var copy = copyObject.GetComponent<GridLevel>();
                Assert.AreEqual(2, copy.ContentCount);
                Assert.AreEqual(1, copy.FaceCount);

                Assert.IsTrue(copy.TryGetContent(new Vector3Int(-1, 1, 1), out var resolvedAnchor, out var slot));
                Assert.AreEqual(anchor, resolvedAnchor);
                Assert.AreEqual(footprint, slot.footprint);

                var face = copy.GetFace(new Vector3Int(0, 0, 0), CellFace.PosY);
                Assert.IsNotNull(face);
                Assert.AreEqual(2, face.rotationStep);
            }
            finally
            {
                Object.DestroyImmediate(copyObject);
            }
        }

        [Test]
        public void Layers_AddFindRemove()
        {
            Assert.AreEqual(1, grid.Layers.Count);
            var added = grid.AddLayer("Props");
            Assert.AreEqual(1, added.id);
            Assert.AreEqual("Props", grid.FindLayer(1).name);

            Assert.IsFalse(grid.RemoveLayer(0));

            grid.SetContent(Vector3Int.zero, Content(Vector3Int.one, layerId: 1));
            Assert.IsFalse(grid.RemoveLayer(1));

            grid.ClearContent(Vector3Int.zero, out _, out _);
            Assert.IsTrue(grid.RemoveLayer(1));
            Assert.IsNull(grid.FindLayer(1));
        }

        [Test]
        public void PruneMissingInstances_RemovesSlotsWhoseInstancesDied()
        {
            var fakePrefab = new GameObject("FakePrefab");
            var contentInstance = new GameObject("ContentInstance");
            var faceInstance = new GameObject("FaceInstance");
            try
            {
                grid.SetContent(Vector3Int.zero, new ContentSlot
                {
                    prefab = fakePrefab,
                    instance = contentInstance,
                    footprint = new Vector3Int(2, 1, 1)
                });
                grid.SetFace(new Vector3Int(3, 0, 3), CellFace.NegZ, new CellSlot
                {
                    prefab = fakePrefab,
                    instance = faceInstance
                });

                Assert.AreEqual(0, grid.CountMissingInstances());

                Object.DestroyImmediate(contentInstance);
                Object.DestroyImmediate(faceInstance);

                Assert.AreEqual(2, grid.CountMissingInstances());
                Assert.AreEqual(2, grid.PruneMissingInstances());
                Assert.AreEqual(0, grid.ContentCount);
                Assert.AreEqual(0, grid.FaceCount);
                Assert.IsFalse(grid.IsContentOccupied(new Vector3Int(1, 0, 0)));
            }
            finally
            {
                if (fakePrefab != null)
                {
                    Object.DestroyImmediate(fakePrefab);
                }
            }
        }

        [Test]
        public void PruneMissingInstances_KeepsDataOnlySlots()
        {
            grid.SetContent(Vector3Int.zero, Content(Vector3Int.one));
            grid.SetFace(Vector3Int.zero, CellFace.PosX, new CellSlot());

            Assert.AreEqual(0, grid.CountMissingInstances());
            Assert.AreEqual(0, grid.PruneMissingInstances());
            Assert.AreEqual(1, grid.ContentCount);
            Assert.AreEqual(1, grid.FaceCount);
        }

        [Test]
        public void ContentCells_EnumeratesAnchorsOnly()
        {
            grid.SetContent(Vector3Int.zero, Content(new Vector3Int(2, 1, 2)));
            grid.SetContent(new Vector3Int(4, 0, 4), Content(Vector3Int.one));

            var cells = grid.ContentCells.Select(e => e.cell).ToList();
            Assert.AreEqual(2, cells.Count);
            Assert.Contains(Vector3Int.zero, cells);
            Assert.Contains(new Vector3Int(4, 0, 4), cells);
        }
    }
}
