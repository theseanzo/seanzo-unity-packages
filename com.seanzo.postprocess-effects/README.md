# Seanzo Post Processes

A library of URP screen-space post-process effects that URP does **not** ship — painterly, stylized, retro, and distortion looks. Each effect is a standard URP `VolumeComponent`; a single `ScriptableRendererFeature` renders them, and the normal URP Volume stack drives them.

Works standalone — these are plain Volume overrides you can set by hand or from code. If the [Post Process Effect Player](https://github.com/theseanzo/seanzo-unity-packages) is also installed, it can animate any of them from data-driven `EffectConfig` assets.

## What's in the box

17 effects, grouped in the **Add Override → SeanFX** menu:

| Category | Effects |
|---|---|
| Painterly | Kuwahara, Oil Paint, Watercolor |
| Retro | Dithering, Halftone, Pixelate |
| Analog | Scanlines, Static Noise, Tracking Distortion, Color Bleed |
| Distortion | Wave, Glitch Block |
| Depth | Fog, Edge Detection |
| Framing | Letterbox, Border |
| Color | Posterize |

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
