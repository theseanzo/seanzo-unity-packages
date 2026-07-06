using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Seanzo.LevelDesign.Editor;

namespace Seanzo.LevelDesign.Tests
{
    public class AxisCorrectionTests
    {
        private const string TempFolder = "Assets/_TempAxisTests";

        private GameObject root;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            if (!AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.CreateFolder("Assets", "_TempAxisTests");
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            AssetDatabase.DeleteAsset(TempFolder);
        }

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("AxisRoot");
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        private GameObject AddChild(Vector3 localPosition)
        {
            var child = GameObject.CreatePrimitive(PrimitiveType.Cube);
            child.transform.SetParent(root.transform, false);
            child.transform.localPosition = localPosition;
            return child;
        }

        [Test]
        public void ZUpFix_MapsBlenderUpToUnityUp()
        {
            Assert.That(
                (AxisCorrectionUtility.ZUpFix * Vector3.forward - Vector3.up).magnitude,
                Is.LessThan(1e-5f));
        }

        [Test]
        public void RotateChildren_RotatesAboutRootOrigin()
        {
            var child = AddChild(new Vector3(0f, 0f, 2f));

            AxisCorrectionUtility.RotateChildren(root, AxisCorrectionUtility.ZUpFix);

            Assert.That((child.transform.localPosition - new Vector3(0f, 2f, 0f)).magnitude, Is.LessThan(1e-4f));
            var up = child.transform.localRotation * Vector3.forward;
            Assert.That((up - Vector3.up).magnitude, Is.LessThan(1e-4f));
        }

        [Test]
        public void RotateChildren_InverseRoundTripsToOriginal()
        {
            var child = AddChild(new Vector3(1f, 2f, 3f));
            child.transform.localRotation = Quaternion.Euler(0f, 30f, 0f);
            var originalPosition = child.transform.localPosition;
            var originalRotation = child.transform.localRotation;

            AxisCorrectionUtility.RotateChildren(root, AxisCorrectionUtility.ZUpFix);
            AxisCorrectionUtility.RotateChildren(root, Quaternion.Inverse(AxisCorrectionUtility.ZUpFix));

            Assert.That((child.transform.localPosition - originalPosition).magnitude, Is.LessThan(1e-4f));
            Assert.That(Quaternion.Angle(child.transform.localRotation, originalRotation), Is.LessThan(0.01f));
        }

        [Test]
        public void CorrectPrefabAsset_PersistsRotationOnWrappedChild()
        {
            AddChild(new Vector3(0f, 0f, 1f));
            var path = $"{TempFolder}/Wrapped.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);

            Assert.IsTrue(AxisCorrectionUtility.CorrectPrefabAsset(path, AxisCorrectionUtility.ZUpFix));

            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var child = asset.transform.GetChild(0);
            Assert.That((child.localPosition - new Vector3(0f, 1f, 0f)).magnitude, Is.LessThan(1e-4f));
            var up = child.localRotation * Vector3.forward;
            Assert.That((up - Vector3.up).magnitude, Is.LessThan(1e-4f));
        }

        [Test]
        public void CorrectPrefabAsset_RootRenderer_IsRefused()
        {
            Object.DestroyImmediate(root);
            root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var path = $"{TempFolder}/RootMesh.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);

            Assert.IsFalse(AxisCorrectionUtility.CorrectPrefabAsset(path, AxisCorrectionUtility.ZUpFix));
        }
    }
}
