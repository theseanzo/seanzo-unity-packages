using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Seanzo.LevelDesign.Editor;

namespace Seanzo.LevelDesign.Tests
{
    public class FootprintDetectorTests
    {
        private const string TempFolder = "Assets/_TempFootprintTests";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            if (!AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.CreateFolder("Assets", "_TempFootprintTests");
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            AssetDatabase.DeleteAsset(TempFolder);
        }

        private static GameObject SaveCubePrefab(string name, Vector3 scale)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.localScale = scale;
            var prefab = PrefabUtility.SaveAsPrefabAsset(cube, $"{TempFolder}/{name}.prefab");
            Object.DestroyImmediate(cube);
            return prefab;
        }

        [Test]
        public void Detect_UnitCube_IsOneCell()
        {
            var prefab = SaveCubePrefab("Unit", Vector3.one);
            Assert.AreEqual(Vector3Int.one, FootprintDetector.Detect(prefab, Vector3.one));
        }

        [Test]
        public void Detect_MultiCellCube_CountsCellsPerAxis()
        {
            var prefab = SaveCubePrefab("Multi", new Vector3(2f, 1f, 2.5f));
            Assert.AreEqual(new Vector3Int(2, 1, 3), FootprintDetector.Detect(prefab, Vector3.one));
        }

        [Test]
        public void Detect_NearModuleBounds_RoundDownWithEpsilon()
        {
            var prefab = SaveCubePrefab("Near", new Vector3(2.005f, 1f, 3.005f));
            Assert.AreEqual(new Vector3Int(2, 1, 3), FootprintDetector.Detect(prefab, Vector3.one));
        }

        [Test]
        public void Detect_SmallerThanModule_ClampsToOne()
        {
            var prefab = SaveCubePrefab("Small", new Vector3(0.4f, 0.4f, 0.4f));
            Assert.AreEqual(Vector3Int.one, FootprintDetector.Detect(prefab, Vector3.one));
        }

        [Test]
        public void Detect_RespectsModuleSize()
        {
            var prefab = SaveCubePrefab("Module2", new Vector3(2f, 2f, 4f));
            Assert.AreEqual(new Vector3Int(1, 1, 2), FootprintDetector.Detect(prefab, new Vector3(2f, 2f, 2f)));
        }

        [Test]
        public void Detect_SeamOverlapOversize_StaysOneModule()
        {
            var prefab = SaveCubePrefab("SeamOverlap", new Vector3(2.033f, 2.033f, 2.033f));
            Assert.AreEqual(Vector3Int.one, FootprintDetector.Detect(prefab, new Vector3(2f, 2f, 2f)));
        }

        [Test]
        public void Detect_NoRenderers_DefaultsToOne()
        {
            var empty = new GameObject("Empty");
            var prefab = PrefabUtility.SaveAsPrefabAsset(empty, $"{TempFolder}/Empty.prefab");
            Object.DestroyImmediate(empty);
            Assert.AreEqual(Vector3Int.one, FootprintDetector.Detect(prefab, Vector3.one));
        }

        [Test]
        public void DetectAuthoredAxes_FloorPanel_FacesUp()
        {
            var prefab = SaveCubePrefab("FlatFloor", new Vector3(2f, 0.2f, 2f));
            FootprintDetector.DetectAuthoredAxes(prefab, out var facing, out var up);
            Assert.AreEqual(AuthoredAxis.PosY, facing);
            Assert.AreEqual(AuthoredAxis.PosZ, up);
        }

        [Test]
        public void DetectAuthoredAxes_WallPanel_FacesForward()
        {
            var prefab = SaveCubePrefab("FlatWall", new Vector3(2f, 2f, 0.2f));
            FootprintDetector.DetectAuthoredAxes(prefab, out var facing, out var up);
            Assert.AreEqual(AuthoredAxis.PosZ, facing);
            Assert.AreEqual(AuthoredAxis.PosY, up);
        }

        [Test]
        public void DetectAuthoredAxes_SidePanel_FacesX()
        {
            var prefab = SaveCubePrefab("FlatSide", new Vector3(0.2f, 2f, 2f));
            FootprintDetector.DetectAuthoredAxes(prefab, out var facing, out var up);
            Assert.AreEqual(AuthoredAxis.PosX, facing);
            Assert.AreEqual(AuthoredAxis.PosY, up);
        }

        [Test]
        public void DetectAuthoredAxes_ChunkyPiece_KeepsWallDefault()
        {
            var prefab = SaveCubePrefab("Chunky", new Vector3(2f, 1.5f, 1.8f));
            FootprintDetector.DetectAuthoredAxes(prefab, out var facing, out var up);
            Assert.AreEqual(AuthoredAxis.PosZ, facing);
            Assert.AreEqual(AuthoredAxis.PosY, up);
        }
    }
}
