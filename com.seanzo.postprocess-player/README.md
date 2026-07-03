# Post Process Effect Player

A reflection-driven player for URP post-processing. It animates any `VolumeComponent` — built-in URP effects or the [Seanzo Post Processes](https://github.com/) pack — from data-driven `EffectConfig` assets, with no per-effect code.

- **`EffectPlayer.Play(EffectConfig)`** — the engine. Reads the config fresh on every call, animates the bound parameters, captures and reverts a neutral so overlapping effects compose.
- **`EffectBank`** — drop it on a GameObject, fill an array of `EffectConfig`s, and wire `PlayEffect(int)` (or `PlayEffect0()`…`PlayEffect9()`) to Input System events or UI buttons in the inspector. No code.

## Install

Package Manager → Add package from git URL:

```
https://github.com/<owner>/<repo>.git?path=Packages/com.seanzo.postprocess-player
```

## Requirements

- Unity 6000.0+
- URP 17.3+

## Samples

Import **Basic Playground** from the package's Samples tab for a primitives scene with `EffectBank` pre-wired to number keys.
