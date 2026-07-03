using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Seanzo.PostProcess.Player
{
    [DisallowMultipleComponent]
    public sealed class EffectOverrideEngine : MonoBehaviour
    {
        Volume _volume;
        VolumeProfile _runtimeProfile;

        void Awake()
        {
            _volume = gameObject.GetComponent<Volume>();
            if (_volume == null)
            {
                _volume = gameObject.AddComponent<Volume>();
            }

            _runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            _volume.profile = _runtimeProfile;
            _volume.isGlobal = true;
            _volume.priority = 100;
            _volume.weight = 1f;
        }

        public T GetOrAdd<T>() where T : VolumeComponent
        {
            if (!_runtimeProfile.TryGet<T>(out T component))
            {
                component = _runtimeProfile.Add<T>();
            }
            return component;
        }

        public VolumeComponent GetOrAdd(Type type)
        {
            if (!_runtimeProfile.TryGet(type, out VolumeComponent component))
            {
                component = _runtimeProfile.Add(type);
            }
            return component;
        }

        void OnDestroy()
        {
            if (_runtimeProfile != null)
            {
                Destroy(_runtimeProfile);
                _runtimeProfile = null;
            }
        }
    }
}
