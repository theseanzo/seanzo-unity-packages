# Seanzo Unity Packages

Editor tooling and URP post-processing packages for Unity 6. Independent packages — use any alone or together.

| Package | What it is | Status |
|---|---|---|
| `com.seanzo.postprocess-player` | A reflection-driven player that animates any URP `VolumeComponent` from data-driven `EffectConfig` assets. Ships an importable sample scene. | Ready |
| `com.seanzo.postprocess-effects` | A library of 17 URP screen-space effects URP doesn't ship (painterly, stylized, retro, distortion), driven by a single renderer feature. | Ready |
| `com.seanzo.grid-level-design` | A scene-view grid painting tool for modular kits: palette-driven placement, box/flood fills, rule tiles, and a pivot editor. | Ready |

## Requirements

- Unity 6000.0+
- URP 17.3+ for the post-processing packages (RenderGraph; for the effect library, compatibility mode must be off). The grid tool has no dependencies.

## Install (Unity Package Manager)

Window → Package Manager → **+** → **Add package from git URL**, then paste:

**Effect Player:**
```
https://github.com/theseanzo/seanzo-unity-packages.git?path=/com.seanzo.postprocess-player
```

**Effect Library:**
```
https://github.com/theseanzo/seanzo-unity-packages.git?path=/com.seanzo.postprocess-effects
```

**Grid Level Design Tool:**
```
https://github.com/theseanzo/seanzo-unity-packages.git?path=/com.seanzo.grid-level-design
```

Pin a version with a tag: `...git?path=/com.seanzo.postprocess-player#v0.1.0`.

---

## Using the Effect Player

The player turns an effect into **data** — which `VolumeComponent`, which parameter, what target value, over what time — and animates it at runtime. No per-effect code.

Two components do the work:

- **`EffectPlayer`** — the engine. `Play(EffectConfig)` reads the config fresh, animates each bound parameter from its live value to the target, and reverts to a captured baseline so overlapping effects compose. It needs an `EffectOverrideEngine` on the same GameObject (added automatically), which owns the runtime global Volume the overrides land on.
- **`EffectBank`** — the trigger. An array of `EffectConfig`s addressed by index, with `PlayEffect(int)` plus parameterless `PlayEffect0()`…`PlayEffect9()` wrappers so you can wire it to Input System events or UI buttons in the inspector.

### Quick start

1. **Add `EffectBank`** to a GameObject. `EffectPlayer` and `EffectOverrideEngine` come with it via `[RequireComponent]`.
2. **Create an effect:** Project window → Create → **Seanzo/Post Process/Effect Config**. Add one or more `ParameterBinding`s:
   - `component` — the `VolumeComponent` type name (built-in URP or custom), e.g. `Bloom`, `Vignette`.
   - `parameter` — the field on it, e.g. `intensity`.
   - `to` — target value. Channel packing: float/int use `x`; bool uses `x` (≥ 0.5 is true); color uses `rgba`; vectors use `xyzw`.
   - `duration`, `curve`, `loop` (`Once`/`Loop`/`PingPong`), `loopCount`, `hold`, `revert`.
3. **Fill `EffectBank.effects`** with your configs.
4. **Trigger:** wire `PlayEffect(int)` (or `PlayEffect0()`…) to an Input System action or a UI Button, or call `EffectPlayer.Play(config)` directly from your own code.
5. **Tune live:** edit a config in the inspector during play, trigger again — the player re-reads it every call, so the change lands on the next trigger.

It works on **any** URP `VolumeComponent` — built-in effects or ones from the effect library below.

### Sample

Package Manager → Effect Player → **Samples** → import **Basic-Playground** for a scene with `EffectBank` pre-wired to number keys, plus example configs on built-in URP effects. Full authoring guide is in the package's `Documentation~/add-your-own-effect.md`.

---

## Using the Effect Library

17 effects with no stock URP equivalent — Kuwahara, Oil Paint, Watercolor, Dithering, Halftone, Pixelate, Scanlines, Static Noise, Tracking Distortion, Color Bleed, Wave, Glitch Block, Fog, Edge Detection, Letterbox, Border, Posterize. Each is a standard URP `VolumeComponent` rendered by one `ScriptableRendererFeature`, grouped in the **Add Override → SeanFX** menu.

1. Add **Seanzo Post Process Feature** to your URP Renderer asset (select the Renderer → Add Renderer Feature).
2. Ensure **compatibility mode is off** (the feature is RenderGraph-only; under compatibility mode it renders nothing).
3. Add a Volume to the scene and override the effect you want.
4. Drive it by hand, from code, or animate it with the Effect Player exactly like any other `VolumeComponent`.

Full setup, requirements, and code examples are in the package's [README](com.seanzo.postprocess-effects/README.md).

---

## Using the Grid Level Design Tool

Paint modular kit prefabs onto a 3D grid in the Scene view. Floors and props place into cells; walls attach to cell faces.

1. Add a **Grid Level** component to an empty GameObject and set **Cell Size** to your kit's module size.
2. Create a palette (Create → **Seanzo/Level Design/Kit Palette**), open **Tools → Seanzo → Kit Palette**, and drag your kit prefabs in. Footprints and facing axes are detected automatically.
3. Select the Grid Level and activate the **Grid Paint** tool: brush, box fill, flood fill, and erase across floor and wall planes, with pick, rotate, layers, and full Undo.
4. Optional: drive a palette entry with a **Rule Kit Tile** so end/middle/corner variants swap automatically as you paint, and use the **Pivot Editor** to fix kit pieces whose pivots fight the grid.

Setup, palette authoring, rule tiles, and the pivot editor are covered in the package's [documentation](com.seanzo.grid-level-design/Documentation~/index.md).
