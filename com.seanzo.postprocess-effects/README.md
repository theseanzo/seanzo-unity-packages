# Seanzo Post Processes

A library of URP screen-space post-process effects that URP does **not** ship — painterly, stylized, retro, and distortion looks. Each effect is a standard URP `VolumeComponent`; a single `ScriptableRendererFeature` renders them, and the normal URP Volume stack drives them.

Works standalone — these are plain Volume overrides you can set by hand or from code. If the [Post Process Effect Player](https://github.com/theseanzo/seanzo-unity-packages) is also installed, it can animate any of them from data-driven `EffectConfig` assets.

## Effects

17 effects with no stock URP equivalent, grouped in the **Add Override → SeanFX** menu. Each links to a reference on the underlying technique.

### Painterly

- **Kuwahara** — An edge-preserving smoothing filter that averages the most uniform region around each pixel, wiping out fine texture while keeping edges crisp for a painterly look. ([Wikipedia](https://en.wikipedia.org/wiki/Kuwahara_filter))
- **Oil Paint** — Rebuilds the image as layered curved brush strokes aligned to image gradients, coarse strokes first then finer ones, for a thick oil-paint look. ([Hertzmann, SIGGRAPH '98](https://history.siggraph.org/learning/painterly-rendering-with-curved-brush-strokes-of-multiple-sizes-by-hertzmann/))
- **Watercolor** — Abstracts the image into flat color regions, then filters them to fake pigment pooling, paper texture, and darkened edges like real wet watercolor. ([Bousseau et al., INRIA](https://maverick.inria.fr/Publications/2006/BKTS06/index.php))

### Retro

- **Dithering** — Tiles a small Bayer threshold matrix across the image, trading smooth gradients for a patterned mix of few colors and hiding banding with a retro feel. ([Wikipedia](https://en.wikipedia.org/wiki/Ordered_dithering))
- **Halftone** — Reproduces continuous tone with a grid of dots that vary in size or spacing, imitating how printed images are made. ([Wikipedia](https://en.wikipedia.org/wiki/Halftone))
- **Pixelate** — Displays the image at a markedly lower resolution so it breaks into large blocky mosaic squares. ([Wikipedia](https://en.wikipedia.org/wiki/Pixelization))

### Analog

- **Scanlines** — Draws evenly spaced dark horizontal lines to mimic the visible rows of a CRT television's raster scan. ([Wikipedia](https://en.wikipedia.org/wiki/Scan_line))
- **Static Noise** — Overlays random flickering dots to recreate the "snow" an analog TV shows with no signal. ([Wikipedia](<https://en.wikipedia.org/wiki/Noise_(video)>))
- **Tracking Distortion** — Recreates VHS tracking errors, where misaligned playback heads make horizontal bands of the picture warp, jitter, or break into static. ([Wikipedia](https://en.wikipedia.org/wiki/Video_tape_tracking))
- **Color Bleed** — Smears color sideways past its edges to imitate the chroma-luma crosstalk and rainbow artifacts of analog composite video. ([Wikipedia](https://en.wikipedia.org/wiki/Dot_crawl))

### Distortion

- **Wave** — Offsets the screen's UV coordinates along a moving sine wave so the image ripples and wobbles like a reflection in water.
- **Glitch Block** — Displaces rectangular blocks of the image to fake the corrupted-data, datamosh look of digital glitch art. ([Wikipedia](https://en.wikipedia.org/wiki/Glitch_art))

### Depth

- **Fog** — Blends distant pixels toward a fog color based on their depth, so far objects fade into haze and gain a sense of distance. ([Wikipedia](https://en.wikipedia.org/wiki/Distance_fog))
- **Edge Detection** — Uses a Sobel gradient on color or depth to find sharp changes and draw outlines around shapes. ([Wikipedia](https://en.wikipedia.org/wiki/Sobel_operator))

### Framing

- **Letterbox** — Adds black bars above and below the image to reframe it into a wider cinematic aspect ratio. ([Wikipedia](<https://en.wikipedia.org/wiki/Letterboxing_(filming)>))
- **Border** — Draws a solid inset frame of fixed thickness around the screen's edges.

### Color

- **Posterize** — Quantizes continuous tones down to a few discrete color levels, producing flat banded regions like a printed poster. ([Wikipedia](https://en.wikipedia.org/wiki/Posterization))

## Install

Package Manager → **+** → **Add package from git URL**:

```
https://github.com/theseanzo/seanzo-unity-packages.git?path=/com.seanzo.postprocess-effects
```

Pin a version with a tag: `...?path=/com.seanzo.postprocess-effects#v0.1.0`.

## URP setup / requirements

Read this first — if you skip it, effects render nothing and there is no error.

- **Unity 6000.0+, URP 17.3+.**
- **RenderGraph required; compatibility mode must be OFF.** The renderer feature runs only on the RenderGraph path. Under compatibility mode it does not execute and your scene renders unchanged. (Project Settings → Graphics / URP asset → uncheck compatibility mode.)
- **Add the renderer feature:** select the URP Renderer asset you use → **Add Renderer Feature → Seanzo Post Process Feature**.
- **Add a Volume** (global, or with a collider) and override the effect you want.

## Use it (no code)

1. Add **Seanzo Post Process Feature** to your active URP Renderer.
2. Add a **Volume** to the scene, create/assign a profile.
3. **Add Override → SeanFX →** pick an effect (e.g. *Painterly/Oil Paint*).
4. Enable it and tune its parameters. The inspector is the tuning surface.

## Drive it from code

Every effect is a normal `VolumeComponent`, so you toggle and animate it like any URP override.

### Plain (no other package needed)

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using Seanzo.PostProcess.Effects;

public class OilPaintToggle : MonoBehaviour
{
    [SerializeField] Volume volume; // a Volume whose profile has the Oil Paint override added

    public void Enable()
    {
        if (volume.profile.TryGet(out OilPaint oil))
        {
            oil.active = true;
            oil.enable.overrideState = true;
            oil.enable.value = true;          // effects render only when enabled
            oil.strokeSize.overrideState = true;
            oil.strokeSize.value = 10f;       // tune any parameter live
        }
    }
}
```

### With the Post Process Effect Player (optional)

If `com.seanzo.postprocess-player` is installed, drive any effect from an `EffectConfig` asset — no per-effect code. The config binds a component by its **simple type name** (the class, not the menu path):

```csharp
using UnityEngine;
using Seanzo.PostProcess.Player;

public class Trigger : MonoBehaviour
{
    [SerializeField] EffectPlayer player;
    [SerializeField] EffectConfig oilPaint; // ParameterBinding.component = "OilPaint"

    public void Play() => player.Play(oilPaint);
}
```

In the config's `ParameterBinding`, `component` is the effect's class name — `OilPaint`, `GlitchBlock`, `Scanlines`, etc. See the Effect Player package for authoring and inspector-only triggering via `EffectBank`.

## License

See [LICENSE.md](LICENSE.md).
