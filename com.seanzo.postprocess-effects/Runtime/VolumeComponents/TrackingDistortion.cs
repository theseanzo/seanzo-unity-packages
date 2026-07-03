using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Seanzo.PostProcess.Effects
{
    [Serializable, VolumeComponentMenu("SeanFX/Analog/Tracking Distortion")]
    public class TrackingDistortion : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter enable = new BoolParameter(false);
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0.02f, 0f, 0.1f);
        public ClampedFloatParameter speed = new ClampedFloatParameter(1.0f, 0f, 5f);
        public ClampedFloatParameter bandHeight = new ClampedFloatParameter(0.05f, 0.01f, 0.2f);

        public bool IsActive() => enable.value && intensity.value > 0;
        public bool IsTileCompatible() => false;
    }
}
