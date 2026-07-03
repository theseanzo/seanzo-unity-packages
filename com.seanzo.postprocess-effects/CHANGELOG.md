# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [0.1.0]

### Added
- Initial release: 17 URP screen-space effects with no stock URP equivalent, rendered through a single `ScriptableRendererFeature` on the RenderGraph path.
  - Painterly: Kuwahara, Oil Paint, Watercolor.
  - Retro: Dithering, Halftone, Pixelate.
  - Analog: Scanlines, Static Noise, Tracking Distortion, Color Bleed.
  - Distortion: Wave, Glitch Block.
  - Depth: Fog, Edge Detection.
  - Framing: Letterbox, Border.
  - Color: Posterize.
- Effects appear under the **SeanFX** Add Override menu and are driven by the standard URP Volume stack.
