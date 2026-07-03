using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Seanzo.PostProcess.Effects
{
    [Serializable, VolumeComponentMenu("SeanFX/Color/Posterize")]
    public class Posterize : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter enable = new BoolParameter(false);
        public ClampedFloatParameter levels = new ClampedFloatParameter(8f, 2f, 32f);

        public bool IsActive() => enable.value;
        public bool IsTileCompatible() => true;
    }
}
