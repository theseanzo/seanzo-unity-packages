# Seanzo Unity Packages

URP post-processing packages for Unity 6.

## Packages

| Package | What it is |
|---|---|
| `com.seanzo.postprocess-player` | A reflection-driven player that animates any URP `VolumeComponent` from data-driven `EffectConfig` assets. Includes an importable sample scene. |
| `com.seanzo.postprocess-effects` | A library of URP screen-space effects URP does not ship (painterly, stylized, retro, distortion), driven by a single renderer feature. |

The two packages are independent — use either alone or together.

## Install (Unity Package Manager)

Window → Package Manager → **+** → **Add package from git URL**, then paste:

**Effect Player:**
```
https://github.com/<owner>/seanzo-unity-packages.git?path=/com.seanzo.postprocess-player
```

**Effect Library:**
```
https://github.com/<owner>/seanzo-unity-packages.git?path=/com.seanzo.postprocess-effects
```

Replace `<owner>` with the GitHub account this repo lives under. To pin a version, append a tag: `...git?path=/com.seanzo.postprocess-player#v0.1.0`.

## Requirements

- Unity 6000.0+
- URP 17.3+ (RenderGraph; compatibility mode off for the effect library)
