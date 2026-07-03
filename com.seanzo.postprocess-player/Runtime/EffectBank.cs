using System;
using UnityEngine;

namespace Seanzo.PostProcess.Player
{
    [RequireComponent(typeof(EffectPlayer))]
    public sealed class EffectBank : MonoBehaviour
    {
        [Tooltip("Effects addressed by index. Wire PlayEffect(i) — or the PlayEffect0..9 shortcuts — to Input System events or UI buttons.")]
        [SerializeField] EffectConfig[] effects = Array.Empty<EffectConfig>();

        EffectPlayer _player;

        public int Count => effects.Length;

        void Awake()
        {
            _player = GetComponent<EffectPlayer>();
        }

        public void PlayEffect(int index)
        {
            if (index < 0 || index >= effects.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"EffectBank on '{name}' has {effects.Length} effect(s); index {index} is out of range.");
            }

            EffectConfig config = effects[index];
            if (config == null)
            {
                throw new InvalidOperationException($"EffectBank on '{name}' has no EffectConfig assigned at index {index}.");
            }

            _player.Play(config);
        }

        public void PlayEffect0() => PlayEffect(0);
        public void PlayEffect1() => PlayEffect(1);
        public void PlayEffect2() => PlayEffect(2);
        public void PlayEffect3() => PlayEffect(3);
        public void PlayEffect4() => PlayEffect(4);
        public void PlayEffect5() => PlayEffect(5);
        public void PlayEffect6() => PlayEffect(6);
        public void PlayEffect7() => PlayEffect(7);
        public void PlayEffect8() => PlayEffect(8);
        public void PlayEffect9() => PlayEffect(9);
    }
}
