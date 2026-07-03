using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Seanzo.PostProcess.Effects
{
    [Serializable, VolumeComponentMenu("SeanFX/Analog/Color Bleed")]
    public class ColorBleed : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter enable = new BoolParameter(false);
        public ClampedFloatParameter amount = new ClampedFloatParameter(0.005f, 0f, 0.05f);

        public bool IsActive() => enable.value && amount.value > 0;
        public bool IsTileCompatible() => false;
    }
}
