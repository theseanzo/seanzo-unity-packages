# Basic Playground

A simple scene wired to show the Post Process Effect Player working on import.

- An `EffectDriver` GameObject carries `EffectBank` (which pulls in `EffectPlayer` + `EffectOverrideEngine`) and `SampleEffectKeys`.
- Press number keys **0–9** in play mode to trigger the effects in the `EffectBank.effects` array.
- The configs bind built-in URP `VolumeComponent`s, so nothing beyond URP is required to see them.
- Edit any `EffectConfig` in the inspector during play, then trigger again — the change lands on the next press.

The scene dressing uses assets from NatureStarterKit2 (a free Unity Asset Store kit), included here so the sample runs on import.

To build your own effects, see the package's `Documentation~/add-your-own-effect.md`.
