using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Seanzo.PostProcess.Effects
{
    [Serializable, VolumeComponentMenu("SeanFX/Painterly/Kuwahara")]
    public class Kuwahara : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter enable = new BoolParameter(false);
        public ClampedIntParameter radius = new ClampedIntParameter(4, 1, 10);

        public bool IsActive() => enable.value;
        public bool IsTileCompatible() => true;
    }
}
