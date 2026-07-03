using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Seanzo.PostProcess.Effects
{
    [Serializable, VolumeComponentMenu("SeanFX/Depth/Edge Detection")]
    public class EdgeDetection : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter enable = new BoolParameter(false);
        public ClampedFloatParameter intensity = new ClampedFloatParameter(3.0f, 0f, 10f);
        public ClampedFloatParameter threshold = new ClampedFloatParameter(0.01f, 0f, 1f);
        public ClampedFloatParameter thickness = new ClampedFloatParameter(1.0f, 1f, 5f);
        public ColorParameter edgeColor = new ColorParameter(Color.black);

        public bool IsActive() => enable.value;
        public bool IsTileCompatible() => false;
    }
}
