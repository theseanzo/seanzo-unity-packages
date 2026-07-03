using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Seanzo.PostProcess.Effects
{
    [Serializable, VolumeComponentMenu("SeanFX/Painterly/Watercolor")]
    public class Watercolor : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter enable = new BoolParameter(false);
        public ClampedFloatParameter bleedRadius = new ClampedFloatParameter(6f, 2f, 12f);
        public ClampedFloatParameter edgeThreshold = new ClampedFloatParameter(0.1f, 0.01f, 0.3f);
        public ClampedFloatParameter saturationBoost = new ClampedFloatParameter(1.2f, 1.0f, 2.0f);
        public ClampedFloatParameter paperTexture = new ClampedFloatParameter(0.3f, 0f, 1f);
        public ClampedFloatParameter wetEdge = new ClampedFloatParameter(0.5f, 0f, 1f);
        public ClampedFloatParameter granulation = new ClampedFloatParameter(0.3f, 0f, 1f);

        public bool IsActive() => enable.value;
        public bool IsTileCompatible() => false;
    }
}
