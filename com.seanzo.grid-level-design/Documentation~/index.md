# Grid Level Design Tool

Paint modular kit prefabs onto a 3D grid in the Scene view. Floors and props place into cells; walls attach to cell faces. Placement is footprint-aware, undoable, and driven by a palette you author from your own kit.

## Install

Package Manager → Add package from git URL:

```
https://github.com/theseanzo/seanzo-unity-packages.git?path=/com.seanzo.grid-level-design
```

Requires Unity 6000.0+. No package dependencies.

## Setup

1. Add a **Grid Level** component to an empty GameObject. Set **Cell Size** to your kit's module size (e.g. 2×2×2 for a 2 m kit). Painted pieces parent under this object.
2. Create a palette: **Create > Seanzo > Level Design > Kit Palette**.
3. Open **Tools > Seanzo > Kit Palette**, assign the palette asset, and set **Module Size** to match the kit.
4. Drag kit prefabs into the window to register them. Each entry gets a detected footprint and auto-detected facing axes; hit **Re-detect All Footprints** after changing Module Size.

Optionally enable **Use Bounds** on the Grid Level to cap the paintable region. Flood fills on an unbounded grid cap at 4096 cells.

### Preparing kit prefabs

- Model assets can't be painted or pivot-edited directly. **Tools > Seanzo > Create Kit Prefabs From Selection** wraps selected models in editable wrapper prefabs.
- Blender-sourced kits that import Z-up: **Tools > Seanzo > Fix Blender Z-Up For Selection** corrects wrappers in place (with an Undo counterpart).

### Palette entries

Select a tile in the palette window to edit it:

- **Allowed Slot** — `Content` (cells, horizontal plane), `Face` (walls, vertical planes), or `Either`.
- **Default Rotation** — quarter turns applied on placement.
- **Authored Facing / Authored Up** — the local axes that point out of and along a painted face. +Y facing tips the piece so its up sticks out of the wall (spikes, panels, decals); +Z keeps it upright and yaws it to the wall (slabs, torches). Auto-detected for flat pieces; uncheck **Auto-detect Axes** to set them manually. Content painting ignores these.
- **Footprint Override** — per-axis cell counts; zero on an axis uses the detected value.
- **Rule Tile** — see below.

## Painting

Select the Grid Level object and activate the **Grid Paint** tool in the Scene view toolbar. The **Seanzo Grid Paint** overlay holds the controls.

- **Paint Plane** — `XZ` paints floors and props into cells; `XY` and `YZ` paint walls onto cell faces. The **Elevation** (horizontal) or **Row** (vertical) field slides the plane along its normal; `-` and `=` step it from the keyboard. For vertical planes, **Paint far side of plane** targets the opposite face of the boundary.
- **Tool** — `Brush`, `Box`, `Flood`, or `Erase`.

| Action | Input |
|---|---|
| Paint | Click or drag |
| Erase | Shift + click/drag (or the Erase tool) |
| Pick entry from scene | Cmd/Ctrl + click |
| Rotate selection | `[` and `]` |
| Step plane slice | `-` and `=` |
| Box fill / box erase | Box tool: drag and release; Shift to erase; Esc cancels |
| Flood fill | Flood tool: click a region |

Previews are green when placeable, red when blocked, and orange for erase. The **Layers** section assigns painted pieces to named layers and toggles their visibility; **View** buttons snap the camera to Top/Front/Right or square-on to the paint plane.

## Rule tiles

A rule tile picks the prefab and rotation from the 3×3 in-plane neighborhood, so runs of wall or floor swap end/middle/corner variants automatically as you paint and erase.

1. Create one: **Create > Seanzo > Level Design > Rule Kit Tile**.
2. Assign it to a palette entry's **Rule Tile** field. The entry's footprint becomes one cell and the inline rule editor appears in the palette window.
3. Set the **Default Prefab** (placed when no rule matches), then **Add Rule** per variant. Click matrix cells to cycle *any* (blank) → *filled* (●) → *empty* (✕); matrix top is +Z on floors and +Y on walls. **Match Rotated** also tries the mask at 90/180/270° and adds the matched rotation to the output, so one end-cap rule covers all four directions.

First matching rule wins; order specific masks before general ones. Neighbors re-resolve automatically after every paint and erase.

## Pivot editor

Fixes pieces whose pivots fight the grid, without touching source meshes. Open a wrapper prefab (prefab stage) and click **Activate Pivot Tool** in the **Seanzo Pivot Editor** overlay.

- Drag the pivot handle, or use **Fit Floor** (bottom-center of bounds), **Fit Wall** (bottom-center of the +Z face), or the per-axis **Min / Center / Max** snaps.
- The **Placement Preview** frame (Floor or Wall, sized by Module Size) shows how the piece will sit in its cell.
- **Apply** offsets the prefab root's children and saves the prefab, so every painted instance updates. **Revert** discards the pending change.

## Notes

- All operations support Undo, including fills, palette edits, and pivot applies.
- Deleting painted instances outside the tool leaves dangling cells; the Grid Level inspector warns and offers **Prune Missing Cells** (also pruned automatically on tool activation).
- Package tests live under `Tests/Editor` and run in the Test Runner when the package is listed in your manifest's `testables`.
