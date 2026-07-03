using Seanzo.PostProcess.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Seanzo.PostProcess.Player.Samples
{
    /// Maps each number-row key to the EffectBank slot of the same index:
    /// key 1 -> slot 1, key 9 -> slot 9, key 0 -> slot 0. Sample-only
    /// convenience; wire your own input to EffectBank.PlayEffect(int) in a real project.
    [RequireComponent(typeof(EffectBank))]
    public sealed class SampleEffectKeys : MonoBehaviour
    {
        EffectBank _bank;

        void Awake()
        {
            _bank = GetComponent<EffectBank>();
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            for (int i = 0; i < DigitKeys.Length; i++)
            {
                if (!keyboard[DigitKeys[i]].wasPressedThisFrame)
                {
                    continue;
                }

                if (i < _bank.Count)
                {
                    _bank.PlayEffect(i);
                }
            }
        }

        static readonly Key[] DigitKeys =
        {
            Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
            Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
        };
    }
}
