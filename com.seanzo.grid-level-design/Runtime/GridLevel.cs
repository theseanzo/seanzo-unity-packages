using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Seanzo.LevelDesign
{
    [DisallowMultipleComponent]
    public sealed class GridLevel : MonoBehaviour, ISerializationCallbackReceiver
    {
        [SerializeField] private Vector3 cellSize = Vector3.one;
        [SerializeField] private bool useBounds;
        [SerializeField] private BoundsInt bounds = new(new Vector3Int(-8, 0, -8), new Vector3Int(16, 8, 16));
        [SerializeField] private List<GridLayerInfo> layers = new()
        {
            new GridLayerInfo { id = 0, name = "Default", visible = true }
        };

        [SerializeField, HideInInspector] private List<ContentEntry> contentEntries = new();
        [SerializeField, HideInInspector] private List<CoverEntry> coverEntries = new();
        [SerializeField, HideInInspector] private List<FaceEntry> faceEntries = new();

        private readonly Dictionary<Vector3Int, ContentSlot> contents = new();
        // Covered cell -> anchor cell; anchors themselves are never cover keys.
        private readonly Dictionary<Vector3Int, Vector3Int> covers = new();
        private readonly Dictionary<(Vector3Int cell, CellFace face), CellSlot> faces = new();

        [Serializable]
        private struct ContentEntry
        {
            public Vector3Int cell;
            public ContentSlot slot;
        }

        [Serializable]
        private struct CoverEntry
        {
            public Vector3Int cell;
            public Vector3Int anchor;
        }

        [Serializable]
        private struct FaceEntry
        {
            public Vector3Int cell;
            public CellFace face;
            public CellSlot slot;
        }

        public Vector3 CellSize
        {
            get => cellSize;
            set => cellSize = Vector3.Max(value, new Vector3(0.001f, 0.001f, 0.001f));
        }

        public bool UseBounds
        {
            get => useBounds;
            set => useBounds = value;
        }

        public BoundsInt Bounds
        {
            get => bounds;
            set => bounds = value;
        }

        public IReadOnlyList<GridLayerInfo> Layers => layers;

        public int ContentCount => contents.Count;

        public int FaceCount => faces.Count;

        public IEnumerable<(Vector3Int cell, ContentSlot slot)> ContentCells
        {
            get
            {
                foreach (var pair in contents)
                {
                    yield return (pair.Key, pair.Value);
                }
            }
        }

        public IEnumerable<(Vector3Int cell, CellFace face, CellSlot slot)> FaceCells
        {
            get
            {
                foreach (var pair in faces)
                {
                    yield return (pair.Key.cell, pair.Key.face, pair.Value);
                }
            }
        }

        public bool InBounds(Vector3Int cell)
        {
            return !useBounds || bounds.Contains(cell);
        }

        public GridLayerInfo FindLayer(int id)
        {
            return layers.FirstOrDefault(l => l.id == id);
        }

        public GridLayerInfo AddLayer(string layerName)
        {
            var id = layers.Count == 0 ? 0 : layers.Max(l => l.id) + 1;
            var layer = new GridLayerInfo { id = id, name = layerName, visible = true };
            layers.Add(layer);
            return layer;
        }

        public bool RemoveLayer(int id)
        {
            if (id == 0)
            {
                return false;
            }
            var layer = FindLayer(id);
            if (layer == null)
            {
                return false;
            }
            var inUse = contents.Values.Any(s => s.layerId == id)
                || faces.Values.Any(s => s.layerId == id);
            if (inUse)
            {
                return false;
            }
            layers.Remove(layer);
            return true;
        }

        public bool IsContentOccupied(Vector3Int cell)
        {
            return contents.ContainsKey(cell) || covers.ContainsKey(cell);
        }

        public bool TryGetContentAnchor(Vector3Int cell, out Vector3Int anchor)
        {
            if (contents.ContainsKey(cell))
            {
                anchor = cell;
                return true;
            }
            return covers.TryGetValue(cell, out anchor);
        }

        public bool TryGetContent(Vector3Int cell, out Vector3Int anchor, out ContentSlot slot)
        {
            slot = null;
            if (!TryGetContentAnchor(cell, out anchor))
            {
                return false;
            }
            slot = contents[anchor];
            return true;
        }

        public bool CanPlaceContent(Vector3Int anchor, Vector3Int footprint)
        {
            if (footprint.x < 1 || footprint.y < 1 || footprint.z < 1)
            {
                return false;
            }
            foreach (var cell in GridMath.FootprintCells(anchor, footprint))
            {
                if (!InBounds(cell) || IsContentOccupied(cell))
                {
                    return false;
                }
            }
            return true;
        }

        public bool SetContent(Vector3Int anchor, ContentSlot slot)
        {
            if (slot == null || !CanPlaceContent(anchor, slot.footprint))
            {
                return false;
            }
            contents[anchor] = slot;
            foreach (var cell in GridMath.FootprintCells(anchor, slot.footprint))
            {
                if (cell != anchor)
                {
                    covers[cell] = anchor;
                }
            }
            return true;
        }

        public bool ClearContent(Vector3Int cell, out Vector3Int anchor, out ContentSlot removed)
        {
            removed = null;
            if (!TryGetContentAnchor(cell, out anchor))
            {
                return false;
            }
            removed = contents[anchor];
            contents.Remove(anchor);
            foreach (var covered in GridMath.FootprintCells(anchor, removed.footprint))
            {
                covers.Remove(covered);
            }
            return true;
        }

        public CellSlot GetFace(Vector3Int cell, CellFace face)
        {
            return faces.TryGetValue((cell, face), out var slot) ? slot : null;
        }

        public bool SetFace(Vector3Int cell, CellFace face, CellSlot slot)
        {
            if (slot == null || !InBounds(cell))
            {
                return false;
            }
            faces[(cell, face)] = slot;
            return true;
        }

        public bool ClearFace(Vector3Int cell, CellFace face, out CellSlot removed)
        {
            if (!faces.TryGetValue((cell, face), out removed))
            {
                return false;
            }
            faces.Remove((cell, face));
            return true;
        }

        public void ClearAll()
        {
            contents.Clear();
            covers.Clear();
            faces.Clear();
        }

        // A painted slot always carries an instance; instance-less slots with a prefab
        // reference mean the instance was deleted behind the tool's back.
        public int CountMissingInstances()
        {
            return contents.Values.Count(s => s.prefab != null && s.instance == null)
                + faces.Values.Count(s => s.prefab != null && s.instance == null);
        }

        public int PruneMissingInstances()
        {
            var staleCells = contents
                .Where(p => p.Value.prefab != null && p.Value.instance == null)
                .Select(p => p.Key)
                .ToList();
            foreach (var cell in staleCells)
            {
                ClearContent(cell, out _, out _);
            }

            var staleFaces = faces
                .Where(p => p.Value.prefab != null && p.Value.instance == null)
                .Select(p => p.Key)
                .ToList();
            foreach (var (cell, face) in staleFaces)
            {
                ClearFace(cell, face, out _);
            }

            return staleCells.Count + staleFaces.Count;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            contentEntries.Clear();
            foreach (var pair in contents)
            {
                contentEntries.Add(new ContentEntry { cell = pair.Key, slot = pair.Value });
            }
            coverEntries.Clear();
            foreach (var pair in covers)
            {
                coverEntries.Add(new CoverEntry { cell = pair.Key, anchor = pair.Value });
            }
            faceEntries.Clear();
            foreach (var pair in faces)
            {
                faceEntries.Add(new FaceEntry { cell = pair.Key.cell, face = pair.Key.face, slot = pair.Value });
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            contents.Clear();
            foreach (var entry in contentEntries)
            {
                contents[entry.cell] = entry.slot;
            }
            covers.Clear();
            foreach (var entry in coverEntries)
            {
                covers[entry.cell] = entry.anchor;
            }
            faces.Clear();
            foreach (var entry in faceEntries)
            {
                faces[(entry.cell, entry.face)] = entry.slot;
            }
        }

        private void OnValidate()
        {
            cellSize = Vector3.Max(cellSize, new Vector3(0.001f, 0.001f, 0.001f));
            if (layers.Count == 0)
            {
                layers.Add(new GridLayerInfo { id = 0, name = "Default", visible = true });
            }
        }
    }
}
