using NUnit.Framework;
using UnityEngine;
using Seanzo.LevelDesign.Editor;

namespace Seanzo.LevelDesign.Tests
{
    public class PivotEditOperationsTests
    {
        private GameObject root;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("PivotRoot");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        private GameObject AddCube(Vector3 localPosition)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(root.transform, false);
            cube.transform.localPosition = localPosition;
            return cube;
        }

        [Test]
        public void ApplyOffset_MovesDirectChildrenByNegativePivot()
        {
            var first = AddCube(new Vector3(1f, 0.5f, 0f));
            var second = AddCube(new Vector3(-1f, 0.5f, 2f));

            PivotEditOperations.ApplyOffset(root, new Vector3(0.5f, 0.5f, 1f));

            Assert.AreEqual(new Vector3(0.5f, 0f, -1f), first.transform.localPosition);
            Assert.AreEqual(new Vector3(-1.5f, 0f, 1f), second.transform.localPosition);
        }

        [Test]
        public void ApplyOffset_BottomCenterPivot_PutsBaseAtOrigin()
        {
            AddCube(new Vector3(2f, 0.5f, 1f));

            Assert.IsTrue(PivotMath.TryCombinedBounds(root, out var bounds));
            var bottomCenter = PivotMath.SnapPoint(bounds, 1, 0, 1);
            PivotEditOperations.ApplyOffset(root, bottomCenter);

            Assert.IsTrue(PivotMath.TryCombinedBounds(root, out var corrected));
            Assert.That(corrected.min.y, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(corrected.center.x, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(corrected.center.z, Is.EqualTo(0f).Within(1e-4f));
        }
    }
}
