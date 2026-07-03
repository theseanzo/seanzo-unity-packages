using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Seanzo.PostProcess.Effects
{
    [Serializable, VolumeComponentMenu("SeanFX/Retro/Pixelate")]
    public class Pixelate : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter enable = new BoolParameter(false);
        public ClampedIntParameter blockSize = new ClampedIntParameter(4, 1, 64);

        public bool IsActive() => enable.value && blockSize.value > 1;
        public bool IsTileCompatible() => true;
    }
}
