# Add your own effect

Adding a post-process effect is data, not code. An effect is an `EffectConfig` asset plus a slot on an `EffectBank`.

## Steps

1. **Create the config** — right-click in the Project window → Create → `Seanzo/Post Process/Effect Config`. This is an `EffectConfig` holding a list of `ParameterBinding`s.
2. **Add a binding** — one binding drives one parameter:
   - `component` — the `VolumeComponent` type name. Any URP `VolumeComponent`, built-in or from a pack like [Seanzo Post Processes](https://github.com/) — e.g. `Bloom`, `Vignette`, `WorkbenchKuwahara`. Simple name or full name both resolve.
   - `parameter` — the parameter field on that component (e.g. `intensity`).
   - `to` — the target value. Channel packing: float/int use `x`; bool uses `x` (≥ 0.5 is true); color uses `rgba`; vectors use `xyzw`. Playback animates from the parameter's live value to `to`.
   - `duration`, `curve` — animation time and easing.
   - `loop` — `Once`, `Loop`, or `PingPong`. `loopCount` is the cycle count when looping (`-1` = infinite).
   - `hold`, `revert` — `Once` only: seconds to hold at the end value, then return to the captured baseline.
3. **Bind to a trigger** — add an `EffectBank` component to a GameObject (it pulls in `EffectPlayer` + `EffectOverrideEngine` automatically), and drop your config into the `effects` array.
4. **Play and trigger** — wire `EffectBank.PlayEffect0()`…`PlayEffect9()` (or `PlayEffect(int)`) to an Input System action or a UI Button in the inspector. Enter play mode and fire it. The call path is simply `EffectBank.PlayEffect(i) → EffectPlayer.Play(config)`.
5. **Iterate live** — edit the config's fields in the inspector during play, then trigger again. The player reads the config fresh on every call, so the edit lands on the next trigger. The inspector is your tuning UI — no custom panel needed.

## Rules the player enforces

Bad configs fail loud with a thrown exception naming the effect and binding, rather than silently doing nothing:

- Unknown `component` or `parameter` name.
- The same parameter bound twice within one effect.
- A `bool` parameter set to `Loop` or `PingPong` (bools can't animate; use `Once`).

## Confirm it works

- [ ] A representative set spans categories (a glow, a vignette, a color grade, a distortion).
- [ ] Each wired trigger plays its config in play mode.
- [ ] Editing a config in the inspector during play changes the next trigger.
- [ ] A deliberately broken config (typo'd name, duplicate parameter, looping bool) throws a clear console error.
