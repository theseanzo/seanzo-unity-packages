# Seanzo Post Processes

A library of URP screen-space post-process effects that URP does **not** ship — painterly, stylized, retro, and distortion looks. Each effect is a standard URP `VolumeComponent`; a single `ScriptableRendererFeature` renders them, and the normal URP Volume stack drives them.

Works with or without the [Post Process Effect Player](https://github.com/) — these are plain Volume overrides, so you can drive them by hand or animate them from the player.

## Install

Package Manager → Add package from git URL:

```
https://github.com/<owner>/<repo>.git?path=Packages/com.seanzo.postprocess-effects
```

## URP setup / requirements

Read this first — if you skip it, effects render nothing and there is no error.

- **Unity 6000.0+, URP 17.3+.**
- **RenderGraph required; compatibility mode must be OFF.** The renderer feature runs only on the RenderGraph path. Under compatibility mode it does not execute and your scene renders unchanged.
- **Add the renderer feature** to the URP Renderer asset you use (Project Settings → Graphics → your Renderer → Add Renderer Feature).
- **Add a Volume** (global or with a collider) and override the effect you want.

## Samples

Import **Effects Showcase** from the package's Samples tab for a primitives scene with the feature added and a spread of effects overridden on a Volume.
