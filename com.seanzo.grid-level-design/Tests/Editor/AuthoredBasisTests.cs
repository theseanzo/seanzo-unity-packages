using NUnit.Framework;
using Seanzo.LevelDesign.Editor;
using UnityEditor;
using UnityEngine;

namespace Seanzo.LevelDesign.Tests
{
    public class AuthoredBasisTests
    {
        private static void AssertDirection(Vector3 expected, Vector3 actual)
        {
            Assert.Less(Vector3.Distance(expected, actual), 0.001f);
        }

        [Test]
        public void AuthoredBasis_WallConvention_IsIdentity()
        {
            var basis = GridMath.AuthoredBasis(AuthoredAxis.PosZ, AuthoredAxis.PosY);
            AssertDirection(Vector3.forward, basis * Vector3.forward);
            AssertDirection(Vector3.up, basis * Vector3.up);
        }

        [Test]
        public void AuthoredBasis_MapsFacingToForwardAndUpToUp()
        {
            var basis = GridMath.AuthoredBasis(AuthoredAxis.PosY, AuthoredAxis.PosZ);
            AssertDirection(Vector3.forward, basis * Vector3.up);
            AssertDirection(Vector3.up, basis * Vector3.forward);
        }

        [Test]
        public void AuthoredBasis_NegativeFacing_MapsToForward()
        {
            var basis = GridMath.AuthoredBasis(AuthoredAxis.NegX, AuthoredAxis.PosY);
            AssertDirection(Vector3.forward, basis * Vector3.left);
            AssertDirection(Vector3.up, basis * Vector3.up);
        }

        [Test]
        public void AuthoredBasis_CollinearUp_FallsBackToCanonicalUp()
        {
            var legacy = GridMath.AuthoredBasis(AuthoredAxis.PosZ, AuthoredAxis.PosZ);
            AssertDirection(Vector3.forward, legacy * Vector3.forward);
            AssertDirection(Vector3.up, legacy * Vector3.up);

            var floorPanel = GridMath.AuthoredBasis(AuthoredAxis.PosY, AuthoredAxis.NegY);
            AssertDirection(Vector3.forward, floorPanel * Vector3.up);
            AssertDirection(Vector3.up, floorPanel * Vector3.forward);
        }
    }

    public class AuthoredBasisPlacementTests
    {
        private const string TempFolder = "Assets/_TempBasisTests";

        private GameObject gridObject;
        private GridLevel grid;
        private GameObject panelPrefab;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            if (!AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.CreateFolder("Assets", "_TempBasisTests");
            }
            // Floor-panel shape: 1 x 0.2 x 1, pivot at the underside center.
            var root = new GameObject("Panel");
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(root.transform);
            body.transform.localScale = new Vector3(1f, 0.2f, 1f);
            body.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            panelPrefab = PrefabUtility.SaveAsPrefabAsset(root, $"{TempFolder}/Panel.prefab");
            Object.DestroyImmediate(root);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            AssetDatabase.DeleteAsset(TempFolder);
        }

        [SetUp]
        public void SetUp()
        {
            gridObject = new GameObject("BasisTestGrid");
            grid = gridObject.AddComponent<GridLevel>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gridObject);
        }

        private KitPaletteEntry FloorPanelEntry()
        {
            return new KitPaletteEntry
            {
                prefab = panelPrefab,
                authoredFacing = AuthoredAxis.PosY,
                authoredUp = AuthoredAxis.PosZ
            };
        }

        private static Bounds InstanceBounds(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds;
        }

        [Test]
        public void PaintFace_FloorPanel_FacesOutOfTheWall()
        {
            var entry = FloorPanelEntry();
            Assert.IsTrue(GridPaintOperations.PaintFace(grid, entry, Vector3Int.zero, CellFace.NegZ, 0, 0));

            var slot = grid.GetFace(Vector3Int.zero, CellFace.NegZ);
            var thinAxis = slot.instance.transform.rotation * Vector3.up;
            AssertDirection(Vector3.back, thinAxis);
        }

        [Test]
        public void PaintFace_FloorPanel_SitsFlushOnTheFaceRect()
        {
            var entry = FloorPanelEntry();
            Assert.IsTrue(GridPaintOperations.PaintFace(grid, entry, Vector3Int.zero, CellFace.NegZ, 0, 0));

            // Cell (0,0,0), NegZ face: the rect spans x/y in [0,1] at z = 0 and
            // the panel extends outward along -Z by its 0.2 thickness.
            var bounds = InstanceBounds(grid.GetFace(Vector3Int.zero, CellFace.NegZ).instance);
            Assert.Less(Vector3.Distance(new Vector3(0f, 0f, -0.2f), bounds.min), 0.001f);
            Assert.Less(Vector3.Distance(new Vector3(1f, 1f, 0f), bounds.max), 0.001f);
        }

        [Test]
        public void PaintFace_FloorPanel_PositiveNormalFace_SitsFlush()
        {
            var entry = FloorPanelEntry();
            Assert.IsTrue(GridPaintOperations.PaintFace(grid, entry, Vector3Int.zero, CellFace.PosX, 0, 0));

            // Cell (0,0,0), PosX face: the rect spans y/z in [0,1] at x = 1 and
            // the panel extends outward along +X.
            var bounds = InstanceBounds(grid.GetFace(Vector3Int.zero, CellFace.PosX).instance);
            Assert.Less(Vector3.Distance(new Vector3(1f, 0f, 0f), bounds.min), 0.001f);
            Assert.Less(Vector3.Distance(new Vector3(1.2f, 1f, 1f), bounds.max), 0.001f);
        }

        [Test]
        public void PaintFace_FloorPanel_HorizontalFace_SitsFlushOnTop()
        {
            var entry = FloorPanelEntry();
            Assert.IsTrue(GridPaintOperations.PaintFace(grid, entry, Vector3Int.zero, CellFace.PosY, 0, 0));

            var bounds = InstanceBounds(grid.GetFace(Vector3Int.zero, CellFace.PosY).instance);
            Assert.Less(Vector3.Distance(new Vector3(0f, 1f, 0f), bounds.min), 0.001f);
            Assert.Less(Vector3.Distance(new Vector3(1f, 1.2f, 1f), bounds.max), 0.001f);
        }

        [Test]
        public void PaintFace_FloorPanel_RotationStep_KeepsFlushInvariants()
        {
            var entry = FloorPanelEntry();
            Assert.IsTrue(GridPaintOperations.PaintFace(grid, entry, Vector3Int.zero, CellFace.NegZ, 1, 0));

            // Whatever the in-plane rotation does to the silhouette, the aligned
            // bounds stay flush at the wall plane, bottom-anchored, and centered
            // across the face tangent.
            var bounds = InstanceBounds(grid.GetFace(Vector3Int.zero, CellFace.NegZ).instance);
            Assert.Less(Mathf.Abs(bounds.max.z), 0.001f);
            Assert.Less(Mathf.Abs(bounds.min.y), 0.001f);
            Assert.Less(Mathf.Abs(bounds.center.x - 0.5f), 0.001f);
        }

        [Test]
        public void PaintFace_WallConventionEntry_KeepsPivotPlacement()
        {
            var entry = new KitPaletteEntry { prefab = panelPrefab };
            Assert.IsTrue(GridPaintOperations.PaintFace(grid, entry, Vector3Int.zero, CellFace.NegZ, 0, 0));

            var instance = grid.GetFace(Vector3Int.zero, CellFace.NegZ).instance;
            Assert.Less(Vector3.Distance(new Vector3(0.5f, 0f, 0f), instance.transform.localPosition), 0.001f);
            Assert.Less(Quaternion.Angle(GridMath.FaceRotation(CellFace.NegZ, 0), instance.transform.localRotation), 0.01f);
        }

        [Test]
        public void Paint_Content_IgnoresAuthoredBasis()
        {
            var entry = FloorPanelEntry();
            entry.detectedFootprint = Vector3Int.one;
            Assert.IsTrue(GridPaintOperations.Paint(grid, entry, Vector3Int.zero, 0, 0));

            Assert.IsTrue(grid.TryGetContent(Vector3Int.zero, out _, out var slot));
            Assert.Less(Quaternion.Angle(Quaternion.identity, slot.instance.transform.localRotation), 0.01f);
        }

        private static void AssertDirection(Vector3 expected, Vector3 actual)
        {
            Assert.Less(Vector3.Distance(expected, actual), 0.001f);
        }
    }
}
