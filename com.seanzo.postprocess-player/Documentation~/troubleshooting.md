# Troubleshooting

## Nothing happens when I trigger an effect

Work down this list — the failure is almost always one of these:

- **No URP renderer feature for the effect.** If the effect comes from a pack like Seanzo Post Processes, its `ScriptableRendererFeature` must be added to the URP Renderer asset you use. Built-in URP effects (Bloom, Vignette, …) don't need this.
- **Compatibility mode is on.** Pack effects that render through a RenderGraph feature do nothing under URP compatibility mode. Turn it off in the URP global settings. (Built-in URP effects are unaffected.)
- **No Volume in the scene, or the parameter isn't overridden.** The player drives its own runtime global `Volume` via `EffectOverrideEngine`, so `EffectPlayer.Play` handles this — but if you're checking an effect by hand, remember URP only applies a `VolumeComponent` when it's *overridden* on an active Volume.
- **The GameObject is disabled.** `EffectPlayer` reverts and clears its state in `OnDisable`; a disabled player plays nothing.

## A config throws in the console

The player fails loud on purpose. The message names the effect and binding:

- `No VolumeComponent type named '…'` — the `component` string doesn't match any loaded `VolumeComponent`. Check spelling; the pack that defines it must be installed.
- `… has no VolumeParameter field named '…'` — the `parameter` string doesn't match a field on that component.
- `… binds '…' more than once` — two bindings in one config drive the same parameter. Split them across configs or remove the duplicate.
- `… binds boolean '…' with loop mode …` — a `bool` parameter can't animate. Use `loop = Once`.

## `EffectBank` throws an index error

- `EffectBank on '…' has N effect(s); index i is out of range` — the wired `PlayEffect(i)` points past the `effects` array. Fill the slot or wire a valid index.
- `EffectBank on '…' has no EffectConfig assigned at index i` — the slot exists but is empty. Assign a config.
