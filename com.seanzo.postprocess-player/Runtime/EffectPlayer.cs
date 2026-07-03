using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Seanzo.PostProcess.Player
{
    [RequireComponent(typeof(EffectOverrideEngine))]
    public sealed class EffectPlayer : MonoBehaviour
    {
        EffectOverrideEngine _engine;

        readonly Dictionary<VolumeParameter, Vector4> _neutral = new();
        readonly Dictionary<VolumeParameter, Coroutine> _running = new();

        void Awake()
        {
            _engine = GetComponent<EffectOverrideEngine>();
        }

        void OnDisable()
        {
            StopAllCoroutines();
            foreach (KeyValuePair<VolumeParameter, Coroutine> entry in _running)
            {
                if (_neutral.TryGetValue(entry.Key, out Vector4 neutral))
                {
                    VolumeParameterAccess.Write(entry.Key, neutral);
                }
            }
            _running.Clear();
            _neutral.Clear();
        }

        public void Play(EffectConfig config)
        {
            if (config == null)
            {
                return;
            }

            var resolved = new List<(VolumeParameter parameter, ParameterBinding binding)>(config.bindings.Count);
            var targeted = new HashSet<VolumeParameter>();

            foreach (ParameterBinding binding in config.bindings)
            {
                Type componentType = VolumeParameterAccess.ResolveComponentType(binding.component);
                VolumeComponent component = _engine.GetOrAdd(componentType);
                VolumeParameter parameter = VolumeParameterAccess.ResolveParameter(component, binding.parameter);

                if (!targeted.Add(parameter))
                {
                    throw new ArgumentException($"Effect '{config.name}' binds '{binding.component}.{binding.parameter}' more than once; a parameter can only be driven by one binding per effect.");
                }

                if (VolumeParameterAccess.IsDiscrete(parameter) && binding.loop != LoopMode.Once)
                {
                    throw new ArgumentException($"Effect '{config.name}' binds boolean '{binding.component}.{binding.parameter}' with loop mode {binding.loop}; boolean parameters only support {LoopMode.Once}.");
                }

                if (!VolumeParameterAccess.IsSupported(parameter))
                {
                    throw new ArgumentException($"Effect '{config.name}' binds '{binding.component}.{binding.parameter}' of unsupported type {parameter.GetType().Name}; only float, int, bool, color, and vector parameters can be driven.");
                }

                resolved.Add((parameter, binding));
            }

            foreach ((VolumeParameter parameter, ParameterBinding binding) in resolved)
            {
                if (_running.TryGetValue(parameter, out Coroutine active) && active != null)
                {
                    return;
                }
            }

            foreach ((VolumeParameter parameter, ParameterBinding binding) in resolved)
            {
                if (!_neutral.ContainsKey(parameter))
                {
                    _neutral[parameter] = VolumeParameterAccess.ReadValue(parameter);
                }

                _running[parameter] = StartCoroutine(PlayBinding(parameter, binding));
            }
        }

        IEnumerator PlayBinding(VolumeParameter parameter, ParameterBinding binding)
        {
            try
            {
                Vector4 neutral = _neutral[parameter];
                Vector4 start = VolumeParameterAccess.ReadValue(parameter);

                if (VolumeParameterAccess.IsDiscrete(parameter) || binding.duration <= 0f)
                {
                    VolumeParameterAccess.Write(parameter, binding.to);
                    if (binding.loop == LoopMode.Once && binding.revert)
                    {
                        if (binding.hold > 0f)
                        {
                            yield return new WaitForSeconds(binding.hold);
                        }
                        VolumeParameterAccess.Write(parameter, neutral);
                    }
                    yield break;
                }

                switch (binding.loop)
                {
                    case LoopMode.Once:
                        yield return Segment(parameter, start, binding.to, binding.duration, binding.curve);
                        if (binding.hold > 0f)
                        {
                            yield return new WaitForSeconds(binding.hold);
                        }
                        if (binding.revert)
                        {
                            yield return Segment(parameter, binding.to, neutral, binding.duration, binding.curve);
                        }
                        break;

                    case LoopMode.Loop:
                        for (int c = 0; binding.loopCount < 0 || c < binding.loopCount; c++)
                        {
                            yield return Segment(parameter, start, binding.to, binding.duration, binding.curve);
                        }
                        break;

                    case LoopMode.PingPong:
                        for (int c = 0; binding.loopCount < 0 || c < binding.loopCount; c++)
                        {
                            yield return Segment(parameter, start, binding.to, binding.duration, binding.curve);
                            yield return Segment(parameter, binding.to, start, binding.duration, binding.curve);
                        }
                        break;
                }
            }
            finally
            {
                _running[parameter] = null;
            }
        }

        static IEnumerator Segment(VolumeParameter parameter, Vector4 from, Vector4 to, float duration, AnimationCurve curve)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float k = curve.Evaluate(t);
                VolumeParameterAccess.Write(parameter, Vector4.LerpUnclamped(from, to, k));
                yield return null;
            }
            VolumeParameterAccess.Write(parameter, to);
        }
    }
}
