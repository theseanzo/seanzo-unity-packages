using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Seanzo.PostProcess.Effects
{
    [Serializable, VolumeComponentMenu("SeanFX/Analog/Static Noise")]
    public class StaticNoise : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter enable = new BoolParameter(false);
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0.02f, 0f, 1f);
        public ClampedFloatParameter blockSize = new ClampedFloatParameter(1f, 0.1f, 10f);
        public BoolParameter colored = new BoolParameter(false);

        public bool IsActive() => enable.value && intensity.value > 0;
        public bool IsTileCompatible() => true;
    }
}
