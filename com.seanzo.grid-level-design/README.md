# Grid Level Design Tool

A scene-view grid painting tool for modular kits. Define a palette from your kit prefabs, then paint them onto floors and walls with footprint-aware placement, box and flood fills, neighbor-aware rule tiles, and an in-place pivot editor.

- **`GridLevel`** — the scene anchor. Holds the cell map and cell size; painted pieces parent under it.
- **Kit Palette window** — author palette entries from prefabs: footprint detection, authored facing/up axes, per-entry rule tiles.
- **Grid Paint tool** — paint, erase, pick, and rotate in the Scene view with a placement ghost; box and flood fills; rule tiles re-resolve neighbors as you paint.
- **Pivot editor** — fix kit pieces whose pivots or axes fight the grid, without touching the source meshes.

## Install

Package Manager → Add package from git URL:

```
https://github.com/theseanzo/seanzo-unity-packages.git?path=/com.seanzo.grid-level-design
```

## Requirements

- Unity 6000.0+

## Docs

See `Documentation~/index.md` for palette setup, painting, fills, rule tiles, and the pivot editor.
