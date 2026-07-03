# Seanzo Unity Packages

URP post-processing packages for Unity 6. Two independent packages — use either alone or together.

| Package | What it is | Status |
|---|---|---|
| `com.seanzo.postprocess-player` | A reflection-driven player that animates any URP `VolumeComponent` from data-driven `EffectConfig` assets. Ships an importable sample scene. | Ready |
| `com.seanzo.postprocess-effects` | A library of URP screen-space effects URP doesn't ship (painterly, stylized, retro, distortion), driven by a single renderer feature. | In development |

## Requirements

- Unity 6000.0+
- URP 17.3+ (RenderGraph; for the effect library, compatibility mode must be off)

## Install (Unity Package Manager)

Window → Package Manager → **+** → **Add package from git URL**, then paste:

**Effect Player:**
```
https://github.com/theseanzo/seanzo-unity-packages.git?path=/com.seanzo.postprocess-player
```

**Effect Library** (once released):
```
https://github.com/theseanzo/seanzo-unity-packages.git?path=/com.seanzo.postprocess-effects
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

> **In development.** The package installs but its effects aren't populated yet. Use the Effect Player with built-in URP effects for now.

When released, the effects are standard URP `VolumeComponent`s rendered by one `ScriptableRendererFeature`. Intended usage:

1. Add the renderer feature to your URP Renderer asset (Project Settings → Graphics → your Renderer → Add Renderer Feature).
2. Ensure **compatibility mode is off** (the feature is RenderGraph-only; under compatibility mode it renders nothing).
3. Add a Volume to the scene and override the effect you want.
4. Drive it by hand, or animate it with the Effect Player exactly like any other `VolumeComponent`.
