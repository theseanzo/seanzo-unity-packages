using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Seanzo.PostProcess.Effects
{
    [Serializable, VolumeComponentMenu("SeanFX/Distortion/Wave")]
    public class WaveDistortion : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter enable = new BoolParameter(false);
        public FloatParameter amplitude = new FloatParameter(0.01f);
        public FloatParameter frequency = new FloatParameter(10.0f);
        public FloatParameter speed = new FloatParameter(2.0f);

        public bool IsActive() => enable.value && amplitude.value > 0;
        public bool IsTileCompatible() => false;
    }
}
