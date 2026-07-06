using NUnit.Framework;
using UnityEngine;

namespace Seanzo.LevelDesign.Tests
{
    public class GridMathFaceTests
    {
        [Test]
        public void FaceAnchorLocal_VerticalFace_IsBottomCenterOfFace()
        {
            var cellSize = new Vector3(2f, 3f, 2f);
            var cell = new Vector3Int(1, 1, 1);

            Assert.AreEqual(new Vector3(3f, 3f, 2f), GridMath.FaceAnchorLocal(cell, CellFace.NegZ, cellSize));
            Assert.AreEqual(new Vector3(3f, 3f, 4f), GridMath.FaceAnchorLocal(cell, CellFace.PosZ, cellSize));
            Assert.AreEqual(new Vector3(2f, 3f, 3f), GridMath.FaceAnchorLocal(cell, CellFace.NegX, cellSize));
            Assert.AreEqual(new Vector3(4f, 3f, 3f), GridMath.FaceAnchorLocal(cell, CellFace.PosX, cellSize));
        }

        [Test]
        public void FaceAnchorLocal_HorizontalFace_IsFaceCenter()
        {
            var cellSize = Vector3.one;
            var cell = new Vector3Int(0, 0, 0);

            Assert.AreEqual(new Vector3(0.5f, 0f, 0.5f), GridMath.FaceAnchorLocal(cell, CellFace.NegY, cellSize));
            Assert.AreEqual(new Vector3(0.5f, 1f, 0.5f), GridMath.FaceAnchorLocal(cell, CellFace.PosY, cellSize));
        }

        [Test]
        public void FaceRotation_AlignsForwardWithFaceNormal()
        {
            foreach (CellFace face in System.Enum.GetValues(typeof(CellFace)))
            {
                var forward = GridMath.FaceRotation(face, 0) * Vector3.forward;
                var expected = (Vector3)GridMath.FaceNormal(face);
                Assert.Less(Vector3.Distance(forward, expected), 0.001f, face.ToString());
            }
        }

        [Test]
        public void FaceRotation_StepTwo_FlipsVerticalFace()
        {
            var forward = GridMath.FaceRotation(CellFace.NegZ, 2) * Vector3.forward;
            Assert.Less(Vector3.Distance(forward, Vector3.forward), 0.001f);
        }

        [Test]
        public void FaceCornersLocal_SpansTheFullFace()
        {
            var corners = new Vector3[4];
            GridMath.FaceCornersLocal(new Vector3Int(0, 0, 0), CellFace.NegZ, new Vector3(2f, 3f, 4f), corners);

            foreach (var corner in corners)
            {
                Assert.AreEqual(0f, corner.z);
            }
            var bounds = new Bounds(corners[0], Vector3.zero);
            foreach (var corner in corners)
            {
                bounds.Encapsulate(corner);
            }
            Assert.AreEqual(new Vector3(2f, 3f, 0f), bounds.size);
        }

        [Test]
        public void TryVerticalPlaneTarget_NearSide_TargetsPlaneCellBoundaryFace()
        {
            var plane = new PaintPlane(PlaneOrientation.XY, 3);
            var planeCell = new Vector3Int(5, 2, 3);

            Assert.IsTrue(GridMath.TryVerticalPlaneTarget(plane, planeCell, false, out var cell, out var face));
            Assert.AreEqual(planeCell, cell);
            Assert.AreEqual(CellFace.NegZ, face);
        }

        [Test]
        public void TryVerticalPlaneTarget_FarSide_TargetsPreviousCellOppositeFace()
        {
            var plane = new PaintPlane(PlaneOrientation.XY, 3);
            var planeCell = new Vector3Int(5, 2, 3);

            Assert.IsTrue(GridMath.TryVerticalPlaneTarget(plane, planeCell, true, out var cell, out var face));
            Assert.AreEqual(new Vector3Int(5, 2, 2), cell);
            Assert.AreEqual(CellFace.PosZ, face);
        }

        [Test]
        public void TryVerticalPlaneTarget_YZPlane_UsesXFaces()
        {
            var plane = new PaintPlane(PlaneOrientation.YZ, 0);
            var planeCell = new Vector3Int(0, 1, 4);

            Assert.IsTrue(GridMath.TryVerticalPlaneTarget(plane, planeCell, false, out _, out var nearFace));
            Assert.AreEqual(CellFace.NegX, nearFace);
            Assert.IsTrue(GridMath.TryVerticalPlaneTarget(plane, planeCell, true, out var farCell, out var farFace));
            Assert.AreEqual(new Vector3Int(-1, 1, 4), farCell);
            Assert.AreEqual(CellFace.PosX, farFace);
        }

        [Test]
        public void TryVerticalPlaneTarget_HorizontalPlane_Fails()
        {
            var plane = new PaintPlane(PlaneOrientation.XZ, 0);
            Assert.IsFalse(GridMath.TryVerticalPlaneTarget(plane, Vector3Int.zero, false, out _, out _));
        }
    }
}
