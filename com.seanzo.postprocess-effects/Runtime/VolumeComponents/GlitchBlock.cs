using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Seanzo.PostProcess.Effects
{
    [Serializable, VolumeComponentMenu("SeanFX/Distortion/Glitch Block")]
    public class GlitchBlock : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter enable = new BoolParameter(false);
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0.1f, 0f, 1f);
        public FloatParameter blockSize = new FloatParameter(16.0f);
        public FloatParameter shiftAmount = new FloatParameter(0.05f);

        public bool IsActive() => enable.value && intensity.value > 0;
        public bool IsTileCompatible() => false;
    }
}
