# Examples

The **Basic Playground** sample ships a set of `EffectConfig`s bound to built-in URP `VolumeComponent`s — no extra packages required. Import it from the package's Samples tab, then trigger each with its number key.

Each entry animates a stock URP effect, proving the player is source-agnostic: the same reflection path drives these and any custom pack.

| Key | Config | URP component | What you see |
|----:|--------|---------------|--------------|
| 0 | Vignette | `Vignette` | edges darken in |
| 1 | Bloom | `Bloom` | highlights bloom |
| 2 | Chromatic Aberration | `ChromaticAberration` | color fringing pulses |
| 3 | Lens Distortion | `LensDistortion` | barrel warp |
| 4 | Color Adjustments (warm) | `ColorAdjustments` | grade shifts warm |
| 5 | Color Adjustments (cool) | `ColorAdjustments` | grade shifts cool |
| 6 | Film Grain | `FilmGrain` | grain rises |
| 7 | Panini Projection | `PaniniProjection` | wide-angle reshape |
| 8 | White Balance | `WhiteBalance` | temperature shift |
| 9 | Color Adjustments (punch) | `ColorAdjustments` | contrast + saturation punch |

<!-- GIF per row to be added from the local hero scene once the sample scene is authored (03D). -->

To build your own, see [Add your own effect](add-your-own-effect.md).
