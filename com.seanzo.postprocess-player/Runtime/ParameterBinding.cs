using System;
using UnityEngine;

namespace Seanzo.PostProcess.Player
{
    [Serializable]
    public class ParameterBinding
    {
        [Tooltip("VolumeComponent type name, e.g. Vignette or Bloom.")]
        public string component;

        [Tooltip("Parameter field name on the component, e.g. intensity.")]
        public string parameter;

        [Tooltip("Target value. float/int use x; bool uses x as 0 or 1; color uses rgba; vector uses xyzw. Playback animates from the parameter's live value to this.")]
        public Vector4 to = Vector4.one;

        [Min(0f)]
        public float duration = 0.5f;

        public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public LoopMode loop = LoopMode.Once;

        [Tooltip("Cycles to run when looping. -1 means infinite.")]
        public int loopCount = -1;

        [Tooltip("Once mode: seconds to hold at the end value before reverting.")]
        [Min(0f)]
        public float hold = 0f;

        [Tooltip("Once mode: return to the captured baseline after the hold.")]
        public bool revert = false;
    }
}
