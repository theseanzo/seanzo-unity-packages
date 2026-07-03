using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Seanzo.PostProcess.Effects
{
    [Serializable, VolumeComponentMenu("SeanFX/Framing/Letterbox")]
    public class Letterbox : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter enable = new BoolParameter(false);
        public FloatParameter aspect = new FloatParameter(2.35f);

        public bool IsActive() => enable.value;
        public bool IsTileCompatible() => false;
    }
}
