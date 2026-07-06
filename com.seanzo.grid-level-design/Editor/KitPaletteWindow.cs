using UnityEditor;
using UnityEngine;

namespace Seanzo.LevelDesign.Editor
{
    public sealed class KitPaletteWindow : EditorWindow
    {
        private const float TileSize = 68f;
        // Approximate layout height of one tile row, used for scroll culling.
        private const float RowHeight = TileSize + 4f;

        private Vector2 scroll;

        [MenuItem("Tools/Seanzo/Kit Palette")]
        public static void Open()
        {
            GetWindow<KitPaletteWindow>("Kit Palette");
        }

        private void OnGUI()
        {
            PaintSession.Palette = (KitPalette)EditorGUILayout.ObjectField(
                "Palette", PaintSession.Palette, typeof(KitPalette), false);
            var palette = PaintSession.Palette;

            if (palette == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a Kit Palette asset (Create > Seanzo > Level Design > Kit Palette), then drop prefabs into this window to register them.",
                    MessageType.Info);
                return;
            }

            EditorGUI.BeginChangeCheck();
            var moduleSize = EditorGUILayout.Vector3Field("Module Size", palette.moduleSize);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(palette, "Edit Kit Palette");
                palette.moduleSize = Vector3.Max(moduleSize, new Vector3(0.001f, 0.001f, 0.001f));
                EditorUtility.SetDirty(palette);
            }
            if (GUILayout.Button("Re-detect All Footprints"))
            {
                Undo.RecordObject(palette, "Edit Kit Palette");
                foreach (var entry in palette.entries)
                {
                    entry.detectedFootprint = FootprintDetector.Detect(entry.prefab, palette.moduleSize);
                    RedetectAuthoredAxes(entry);
                }
                EditorUtility.SetDirty(palette);
            }

            EditorGUILayout.Space();
            DrawEntryGrid(palette);
            EditorGUILayout.Space();
            DrawSelectedEntry(palette);
            DrawFooter();
            HandleDragAndDrop(palette);

            if (AssetPreview.IsLoadingAssetPreviews())
            {
                Repaint();
            }
        }

        private void DrawEntryGrid(KitPalette palette)
        {
            if (palette.entries.Count == 0)
            {
                EditorGUILayout.HelpBox("Drop prefabs into this window to register them.", MessageType.Info);
                return;
            }

            // The preview cache is editor-wide, LRU, and shared with other windows.
            // Smaller than one preview per tile means previews regenerate forever
            // and IsLoadingAssetPreviews never settles, so the window repaints in
            // a loop. Reassert the size every pass; another window may shrink it.
            AssetPreview.SetPreviewTextureCacheSize(Mathf.Max(128, palette.entries.Count + 16));

            var columns = Mathf.Max(1, (int)(position.width / (TileSize + 8f)));
            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (var i = 0; i < palette.entries.Count; i += columns)
            {
                var rowTop = (i / columns) * RowHeight;
                var rowVisible = rowTop + 2f * RowHeight >= scroll.y
                    && rowTop <= scroll.y + position.height + RowHeight;
                EditorGUILayout.BeginHorizontal();
                for (var c = 0; c < columns && i + c < palette.entries.Count; c++)
                {
                    DrawEntryTile(palette, i + c, rowVisible);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private static void DrawEntryTile(KitPalette palette, int index, bool requestPreview)
        {
            var entry = palette.entries[index];
            var selected = PaintSession.EntryIndex == index;

            var preview = requestPreview && entry.prefab != null
                ? AssetPreview.GetAssetPreview(entry.prefab) ?? AssetPreview.GetMiniThumbnail(entry.prefab)
                : null;
            var label = entry.prefab != null ? entry.prefab.name : "(missing)";
            var content = preview != null ? new GUIContent(preview, label) : new GUIContent(label);

            var previous = GUI.backgroundColor;
            if (selected)
            {
                GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
            }
            if (GUILayout.Button(content, GUILayout.Width(TileSize), GUILayout.Height(TileSize)))
            {
                PaintSession.SelectEntry(index);
            }
            GUI.backgroundColor = previous;
        }

        // Manual facing choices survive footprint re-detection; only entries still
        // marked auto re-detect their axes.
        private static void RedetectAuthoredAxes(KitPaletteEntry entry)
        {
            if (entry.authoredAxesAuto)
            {
                FootprintDetector.DetectAuthoredAxes(entry.prefab, out entry.authoredFacing, out entry.authoredUp);
            }
        }

        private static void DrawSelectedEntry(KitPalette palette)
        {
            var entry = PaintSession.ActiveEntry;
            if (entry == null)
            {
                return;
            }

            EditorGUILayout.LabelField(entry.prefab != null ? entry.prefab.name : "(missing prefab)", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            var allowedSlot = (SlotKind)EditorGUILayout.EnumPopup("Allowed Slot", entry.allowedSlot);
            var defaultRotation = EditorGUILayout.IntSlider("Default Rotation", entry.defaultRotationStep, 0, 3);
            var authoredFacing = (AuthoredAxis)EditorGUILayout.EnumPopup(
                new GUIContent("Authored Facing", "Local axis that points out of a painted face. +Y tips the piece so its up sticks out of the wall (spikes, floor panels, decals). +Z keeps it upright and yaws it to the wall (wall slabs, torches). Content painting ignores this."),
                entry.authoredFacing);
            var authoredUp = (AuthoredAxis)EditorGUILayout.EnumPopup(
                new GUIContent("Authored Up", "Local axis that points along a painted face's up. Ignored when collinear with the facing."),
                entry.authoredUp);
            var axesAuto = EditorGUILayout.ToggleLeft(
                new GUIContent("Auto-detect Axes", "Re-detection updates the axes while checked. Editing either axis unchecks this; re-checking re-detects immediately."),
                entry.authoredAxesAuto);
            EditorGUILayout.LabelField("Detected Footprint", entry.detectedFootprint.ToString());
            var overrideFootprint = EditorGUILayout.Vector3IntField(
                new GUIContent("Footprint Override", "Zero on any axis uses the detected footprint."),
                entry.footprintOverride);
            var ruleTile = (RuleKitTile)EditorGUILayout.ObjectField(
                new GUIContent("Rule Tile", "When set, painting resolves the piece from neighbor rules and the footprint is one cell."),
                entry.ruleTile, typeof(RuleKitTile), false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(palette, "Edit Kit Palette");
                entry.allowedSlot = allowedSlot;
                entry.defaultRotationStep = defaultRotation;
                var axesEdited = authoredFacing != entry.authoredFacing || authoredUp != entry.authoredUp;
                var toggleChanged = axesAuto != entry.authoredAxesAuto;
                entry.authoredFacing = authoredFacing;
                entry.authoredUp = authoredUp;
                if (toggleChanged)
                {
                    entry.authoredAxesAuto = axesAuto;
                    if (axesAuto)
                    {
                        FootprintDetector.DetectAuthoredAxes(entry.prefab, out entry.authoredFacing, out entry.authoredUp);
                    }
                }
                else if (axesEdited)
                {
                    entry.authoredAxesAuto = false;
                }
                entry.footprintOverride = Vector3Int.Max(overrideFootprint, Vector3Int.zero);
                entry.ruleTile = ruleTile;
                EditorUtility.SetDirty(palette);
            }
            if (entry.HasOverride && entry.footprintOverride != entry.detectedFootprint)
            {
                EditorGUILayout.HelpBox(
                    $"Override {entry.footprintOverride} differs from detected {entry.detectedFootprint}.",
                    MessageType.None);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Re-detect Footprint"))
            {
                Undo.RecordObject(palette, "Edit Kit Palette");
                entry.detectedFootprint = FootprintDetector.Detect(entry.prefab, palette.moduleSize);
                RedetectAuthoredAxes(entry);
                EditorUtility.SetDirty(palette);
            }
            if (GUILayout.Button("Remove Entry"))
            {
                Undo.RecordObject(palette, "Edit Kit Palette");
                palette.entries.RemoveAt(PaintSession.EntryIndex);
                PaintSession.EntryIndex = -1;
                EditorUtility.SetDirty(palette);
            }
            EditorGUILayout.EndHorizontal();

            if (entry.ruleTile != null)
            {
                DrawRuleTileEditor(entry.ruleTile);
            }
        }

        private static void DrawRuleTileEditor(RuleKitTile tile)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Rules — {tile.name}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "First matching rule wins. Matrix top = +Z on floors, +Y on walls.", EditorStyles.miniLabel);

            EditorGUI.BeginChangeCheck();
            var defaultPrefab = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Default Prefab", "Placed when no rule matches."),
                tile.defaultPrefab, typeof(GameObject), false);
            var defaultRotation = EditorGUILayout.IntSlider("Default Rotation", tile.defaultRotationStep, 0, 3);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(tile, "Edit Rule Kit Tile");
                tile.defaultPrefab = defaultPrefab;
                tile.defaultRotationStep = defaultRotation;
                EditorUtility.SetDirty(tile);
            }

            var removeIndex = -1;
            for (var i = 0; i < tile.rules.Count; i++)
            {
                var rule = tile.rules[i];
                if (rule.neighbors == null || rule.neighbors.Length != 8)
                {
                    Undo.RecordObject(tile, "Repair Rule Kit Tile");
                    rule.neighbors = new NeighborRule[8];
                    EditorUtility.SetDirty(tile);
                }

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                DrawNeighborMatrix(tile, rule);
                EditorGUILayout.BeginVertical();
                EditorGUI.BeginChangeCheck();
                var prefab = (GameObject)EditorGUILayout.ObjectField(rule.prefab, typeof(GameObject), false);
                var rotation = EditorGUILayout.IntSlider("Rotation", rule.rotationStep, 0, 3);
                var matchRotated = EditorGUILayout.ToggleLeft(
                    new GUIContent("Match Rotated", "Also try the mask at 90/180/270 degrees; the matched rotation adds to the output rotation."),
                    rule.matchRotated);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(tile, "Edit Rule Kit Tile");
                    rule.prefab = prefab;
                    rule.rotationStep = rotation;
                    rule.matchRotated = matchRotated;
                    EditorUtility.SetDirty(tile);
                }
                if (GUILayout.Button("Remove Rule"))
                {
                    removeIndex = i;
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }

            if (removeIndex >= 0)
            {
                Undo.RecordObject(tile, "Edit Rule Kit Tile");
                tile.rules.RemoveAt(removeIndex);
                EditorUtility.SetDirty(tile);
            }
            if (GUILayout.Button("Add Rule"))
            {
                Undo.RecordObject(tile, "Edit Rule Kit Tile");
                tile.rules.Add(new KitTileRule());
                EditorUtility.SetDirty(tile);
            }
        }

        private static void DrawNeighborMatrix(RuleKitTile tile, KitTileRule rule)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(3 * 24f));
            for (var row = 0; row < 3; row++)
            {
                EditorGUILayout.BeginHorizontal();
                for (var col = 0; col < 3; col++)
                {
                    if (row == 1 && col == 1)
                    {
                        using (new EditorGUI.DisabledScope(true))
                        {
                            GUILayout.Button(" ", GUILayout.Width(22f), GUILayout.Height(22f));
                        }
                        continue;
                    }
                    var index = NeighborIndex(row, col);
                    var condition = rule.NeighborAt(index);
                    var label = condition switch
                    {
                        NeighborRule.Filled => "●",
                        NeighborRule.Empty => "✕",
                        _ => " "
                    };
                    if (GUILayout.Button(
                            new GUIContent(label, "Cycle: any, filled, empty"),
                            GUILayout.Width(22f), GUILayout.Height(22f)))
                    {
                        Undo.RecordObject(tile, "Edit Rule Kit Tile");
                        rule.neighbors[index] = (NeighborRule)(((int)condition + 1) % 3);
                        EditorUtility.SetDirty(tile);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        // Maps the 3x3 matrix cell (row 0 = top = +b) to a RuleMatcher.Offsets index.
        private static int NeighborIndex(int row, int col)
        {
            return row switch
            {
                0 => col,
                1 => col == 0 ? 3 : 4,
                _ => 5 + col
            };
        }

        private static void DrawFooter()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                $"Rotation: {PaintSession.RotationStep * 90}°   |   Paint: click/drag   Erase: Shift   Pick: Cmd/Ctrl   Rotate: [ ]",
                EditorStyles.miniLabel);
        }

        private void HandleDragAndDrop(KitPalette palette)
        {
            var e = Event.current;
            if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform)
            {
                return;
            }
            var hasPrefab = false;
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is GameObject go && PrefabUtility.GetPrefabAssetType(go) != PrefabAssetType.NotAPrefab)
                {
                    hasPrefab = true;
                    break;
                }
            }
            if (!hasPrefab)
            {
                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                Undo.RecordObject(palette, "Register Palette Prefabs");
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is not GameObject go
                        || PrefabUtility.GetPrefabAssetType(go) == PrefabAssetType.NotAPrefab
                        || palette.entries.Exists(entry => entry.prefab == go))
                    {
                        continue;
                    }
                    FootprintDetector.DetectAuthoredAxes(go, out var facing, out var up);
                    palette.entries.Add(new KitPaletteEntry
                    {
                        prefab = go,
                        detectedFootprint = FootprintDetector.Detect(go, palette.moduleSize),
                        authoredFacing = facing,
                        authoredUp = up
                    });
                }
                EditorUtility.SetDirty(palette);
            }
            e.Use();
        }
    }
}
