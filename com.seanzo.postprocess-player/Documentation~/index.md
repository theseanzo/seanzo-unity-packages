# Post Process Effect Player

A reflection-driven player for URP post-processing. You describe an effect as data — which `VolumeComponent`, which parameter, what target value, over what time — and the player animates it at runtime. No per-effect code.

## The mental model

Two components do the work:

- **`EffectPlayer`** — the engine. `Play(EffectConfig)` reads the config fresh, animates each bound parameter from its live value to the target, and (for one-shots that opt in) reverts to a captured baseline so overlapping effects compose. It needs an `EffectOverrideEngine` on the same GameObject, which owns the runtime global `Volume` the overrides land on. `[RequireComponent]` adds it for you.
- **`EffectBank`** — the trigger. An array of `EffectConfig`s addressed by index. Wire `PlayEffect(0)`…`PlayEffect(9)` (or the parameterless `PlayEffect0()`…`PlayEffect9()` shortcuts) to Input System events or UI buttons in the inspector. No scripting.

An **`EffectConfig`** is a ScriptableObject: a list of `ParameterBinding`s. One binding drives one parameter.

## Where to go next

- [Add your own effect](add-your-own-effect.md) — the authoring loop, end to end.
- [Examples](examples.md) — the configs shipped in the Basic Playground sample.
- [Troubleshooting](troubleshooting.md) — when nothing renders or a config throws.

## Install

Package Manager → Add package from git URL:

```
https://github.com/<owner>/<repo>.git?path=Packages/com.seanzo.postprocess-player
```
