using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Seanzo.PostProcess.Effects
{
    [Serializable, VolumeComponentMenu("SeanFX/Painterly/Oil Paint")]
    public class OilPaint : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter enable = new BoolParameter(false);
        public ClampedFloatParameter strokeSize = new ClampedFloatParameter(6.0f, 4f, 16f);
        public ClampedFloatParameter smoothness = new ClampedFloatParameter(1.5f, 0.1f, 3.0f);
        public ClampedFloatParameter directionWeight = new ClampedFloatParameter(0.8f, 0f, 1f);
        public ClampedFloatParameter sharpness = new ClampedFloatParameter(3.0f, 1f, 10f);

        public bool IsActive() => enable.value;
        public bool IsTileCompatible() => true;
    }
}
