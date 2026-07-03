using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Seanzo.PostProcess.Effects
{
    [Serializable, VolumeComponentMenu("SeanFX/Retro/Halftone")]
    public class Halftone : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter enable = new BoolParameter(false);
        public FloatParameter dotSize = new FloatParameter(30.0f);
        public ClampedFloatParameter angle = new ClampedFloatParameter(45.0f, 0.0f, 360.0f);
        public ClampedFloatParameter intensity = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        public bool IsActive() => enable.value;
        public bool IsTileCompatible() => true;
    }
}
