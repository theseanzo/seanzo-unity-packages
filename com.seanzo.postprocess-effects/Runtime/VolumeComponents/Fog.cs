using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Seanzo.PostProcess.Effects
{
    [Serializable, VolumeComponentMenu("SeanFX/Depth/Fog")]
    public class Fog : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter enable = new BoolParameter(false);
        public ClampedFloatParameter density = new ClampedFloatParameter(1.0f, 0f, 10f);
        public FloatParameter startDistance = new FloatParameter(0.3f);
        public FloatParameter endDistance = new FloatParameter(1.0f);
        public ColorParameter color = new ColorParameter(new Color(0.7f, 0.8f, 0.9f));

        public bool IsActive() => enable.value && density.value > 0;
        public bool IsTileCompatible() => false;
    }
}
