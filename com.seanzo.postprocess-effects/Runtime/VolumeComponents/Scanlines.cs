using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Seanzo.PostProcess.Effects
{
    [Serializable, VolumeComponentMenu("SeanFX/Analog/Scanlines")]
    public class Scanlines : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter enable = new BoolParameter(false);
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0.3f, 0f, 1f);
        public FloatParameter frequency = new FloatParameter(480f);
        public ClampedFloatParameter thickness = new ClampedFloatParameter(0.5f, 0f, 1f);
        public FloatParameter speed = new FloatParameter(15f);

        public bool IsActive() => enable.value && intensity.value > 0;
        public bool IsTileCompatible() => true;
    }
}
