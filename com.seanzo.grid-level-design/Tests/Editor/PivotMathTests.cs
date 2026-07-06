using NUnit.Framework;
using UnityEngine;

namespace Seanzo.LevelDesign.Tests
{
    public class PivotMathTests
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
        public void TryCombinedBounds_NoRenderers_ReturnsFalse()
        {
            Assert.IsFalse(PivotMath.TryCombinedBounds(root, out _));
        }

        [Test]
        public void TryCombinedBounds_CombinesMultipleRenderers()
        {
            AddCube(new Vector3(1f, 0f, 0f));
            AddCube(new Vector3(-1f, 0f, 0f));

            Assert.IsTrue(PivotMath.TryCombinedBounds(root, out var bounds));
            Assert.AreEqual(Vector3.zero, bounds.center);
            Assert.AreEqual(new Vector3(3f, 1f, 1f), bounds.size);
        }

        [Test]
        public void TryCombinedBounds_IgnoresRootWorldPlacement()
        {
            AddCube(new Vector3(0f, 0.5f, 0f));
            root.transform.SetPositionAndRotation(new Vector3(10f, 5f, 2f), Quaternion.Euler(0f, 45f, 0f));

            Assert.IsTrue(PivotMath.TryCombinedBounds(root, out var bounds));
            Assert.That(bounds.center.x, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(bounds.center.y, Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(bounds.size.x, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void SnapPoint_MapsStepsToMinCenterMax()
        {
            var bounds = new Bounds(Vector3.zero, new Vector3(2f, 4f, 6f));

            Assert.AreEqual(new Vector3(-1f, 0f, 3f), PivotMath.SnapPoint(bounds, 0, 1, 2));
        }
    }
}
