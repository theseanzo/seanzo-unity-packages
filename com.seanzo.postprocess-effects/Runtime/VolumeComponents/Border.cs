using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Seanzo.PostProcess.Effects
{
    [Serializable, VolumeComponentMenu("SeanFX/Framing/Border")]
    public class Border : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter enable = new BoolParameter(false);
        public ClampedFloatParameter width = new ClampedFloatParameter(0.02f, 0f, 0.5f);
        public ColorParameter color = new ColorParameter(Color.black);

        public bool IsActive() => enable.value && width.value > 0;
        public bool IsTileCompatible() => false;
    }
}
