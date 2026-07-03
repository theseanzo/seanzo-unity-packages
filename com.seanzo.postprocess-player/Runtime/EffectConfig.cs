using System.Collections.Generic;
using UnityEngine;

namespace Seanzo.PostProcess.Player
{
    [CreateAssetMenu(menuName = "Seanzo/Post Process/Effect Config", fileName = "EffectConfig")]
    public sealed class EffectConfig : ScriptableObject
    {
        public List<ParameterBinding> bindings = new();
    }
}
