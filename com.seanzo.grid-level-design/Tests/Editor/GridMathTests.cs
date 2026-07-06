using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Seanzo.LevelDesign.Tests
{
    public class GridMathTests
    {
        [Test]
        public void CellCenterLocal_UsesCellSize()
        {
            var center = GridMath.CellCenterLocal(new Vector3Int(1, 0, -1), new Vector3(2f, 3f, 4f));
            Assert.AreEqual(new Vector3(3f, 1.5f, -2f), center);
        }

        [Test]
        public void CellMinLocal_UsesCellSize()
        {
            var min = GridMath.CellMinLocal(new Vector3Int(2, -1, 0), new Vector3(2f, 3f, 4f));
            Assert.AreEqual(new Vector3(4f, -3f, 0f), min);
        }

        [Test]
        public void LocalToCell_FloorsNegativeCoordinates()
        {
            var cell = GridMath.LocalToCell(new Vector3(-0.1f, 0.5f, -2.5f), Vector3.one);
            Assert.AreEqual(new Vector3Int(-1, 0, -3), cell);
        }

        [Test]
        public void LocalToCell_RoundTripsCellCenter()
        {
            var cellSize = new Vector3(2f, 1.5f, 3f);
            var cell = new Vector3Int(4, -2, 7);
            var center = GridMath.CellCenterLocal(cell, cellSize);
            Assert.AreEqual(cell, GridMath.LocalToCell(center, cellSize));
        }

        [Test]
        public void FaceNormal_And_Opposite_ArePairedInverses()
        {
            foreach (CellFace face in System.Enum.GetValues(typeof(CellFace)))
            {
                var opposite = GridMath.Opposite(face);
                Assert.AreEqual(GridMath.FaceNormal(face) * -1, GridMath.FaceNormal(opposite));
                Assert.AreEqual(face, GridMath.Opposite(opposite));
            }
        }

        [Test]
        public void TryFaceBetween_AdjacentCells_ReturnsFace()
        {
            var found = GridMath.TryFaceBetween(new Vector3Int(0, 0, 0), new Vector3Int(0, 1, 0), out var face);
            Assert.IsTrue(found);
            Assert.AreEqual(CellFace.PosY, face);
        }

        [Test]
        public void TryFaceBetween_NonAdjacentCells_Fails()
        {
            Assert.IsFalse(GridMath.TryFaceBetween(Vector3Int.zero, new Vector3Int(1, 1, 0), out _));
            Assert.IsFalse(GridMath.TryFaceBetween(Vector3Int.zero, new Vector3Int(2, 0, 0), out _));
            Assert.IsFalse(GridMath.TryFaceBetween(Vector3Int.zero, Vector3Int.zero, out _));
        }

        [TestCase(PlaneOrientation.XZ, 1)]
        [TestCase(PlaneOrientation.XY, 2)]
        [TestCase(PlaneOrientation.YZ, 0)]
        public void InPlaneNeighbors_ReturnsEightUniqueCellsInPlane(PlaneOrientation orientation, int normalAxis)
        {
            var cell = new Vector3Int(3, -1, 5);
            var neighbors = GridMath.InPlaneNeighbors(cell, orientation).ToList();
            Assert.AreEqual(8, neighbors.Count);
            Assert.AreEqual(8, new HashSet<Vector3Int>(neighbors).Count);
            foreach (var neighbor in neighbors)
            {
                Assert.AreEqual(cell[normalAxis], neighbor[normalAxis]);
                Assert.AreNotEqual(cell, neighbor);
            }
        }

        [Test]
        public void FootprintCells_CountMatchesVolume()
        {
            var cells = GridMath.FootprintCells(new Vector3Int(1, 2, 3), new Vector3Int(2, 1, 3)).ToList();
            Assert.AreEqual(6, cells.Count);
            Assert.Contains(new Vector3Int(1, 2, 3), cells);
            Assert.Contains(new Vector3Int(2, 2, 5), cells);
        }

        [TestCase(0, 2, 1, 3, 2, 1, 3)]
        [TestCase(1, 2, 1, 3, 3, 1, 2)]
        [TestCase(2, 2, 1, 3, 2, 1, 3)]
        [TestCase(3, 2, 1, 3, 3, 1, 2)]
        [TestCase(-1, 2, 1, 3, 3, 1, 2)]
        public void RotateFootprint_SwapsHorizontalAxesOnOddSteps(
            int step, int x, int y, int z, int ex, int ey, int ez)
        {
            var rotated = GridMath.RotateFootprint(new Vector3Int(x, y, z), step);
            Assert.AreEqual(new Vector3Int(ex, ey, ez), rotated);
        }
    }
}
