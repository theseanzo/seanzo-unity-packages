using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace Seanzo.PostProcess.Player
{
    public static class VolumeParameterAccess
    {
        static readonly Dictionary<string, Type> TypeCache = new();
        static readonly HashSet<string> FailedTypeNames = new();

        public static Type ResolveComponentType(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("VolumeComponent name is null or empty.", nameof(name));
            }

            if (TypeCache.TryGetValue(name, out Type cached))
            {
                return cached;
            }

            if (FailedTypeNames.Contains(name))
            {
                throw new ArgumentException($"No VolumeComponent type named '{name}' exists in any loaded assembly.", nameof(name));
            }

            Type byFullName = null;
            Type bySimpleName = null;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }

                foreach (Type type in types)
                {
                    if (type == null || type.IsAbstract || !typeof(VolumeComponent).IsAssignableFrom(type))
                    {
                        continue;
                    }

                    if (type.FullName == name)
                    {
                        byFullName = type;
                        break;
                    }

                    if (bySimpleName == null && type.Name == name)
                    {
                        bySimpleName = type;
                    }
                }

                if (byFullName != null)
                {
                    break;
                }
            }

            Type resolved = byFullName ?? bySimpleName;
            if (resolved == null)
            {
                FailedTypeNames.Add(name);
                throw new ArgumentException($"No VolumeComponent type named '{name}' exists in any loaded assembly.", nameof(name));
            }

            TypeCache[name] = resolved;
            return resolved;
        }

        public static VolumeParameter ResolveParameter(VolumeComponent component, string parameterName)
        {
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }
            if (string.IsNullOrEmpty(parameterName))
            {
                throw new ArgumentException("Parameter name is null or empty.", nameof(parameterName));
            }

            FieldInfo field = component.GetType().GetField(parameterName, BindingFlags.Public | BindingFlags.Instance);
            VolumeParameter parameter = field?.GetValue(component) as VolumeParameter;
            if (parameter == null)
            {
                throw new ArgumentException($"'{component.GetType().Name}' has no VolumeParameter field named '{parameterName}'.", nameof(parameterName));
            }

            return parameter;
        }

        public static bool IsDiscrete(VolumeParameter parameter)
        {
            return parameter is VolumeParameter<bool>;
        }

        public static bool IsSupported(VolumeParameter parameter)
        {
            return parameter is VolumeParameter<float>
                || parameter is VolumeParameter<int>
                || parameter is VolumeParameter<bool>
                || parameter is VolumeParameter<Color>
                || parameter is VolumeParameter<Vector2>
                || parameter is VolumeParameter<Vector3>
                || parameter is VolumeParameter<Vector4>;
        }

        public static Vector4 ReadValue(VolumeParameter parameter)
        {
            switch (parameter)
            {
                case VolumeParameter<float> f: return new Vector4(f.value, 0f, 0f, 0f);
                case VolumeParameter<int> i: return new Vector4(i.value, 0f, 0f, 0f);
                case VolumeParameter<bool> b: return new Vector4(b.value ? 1f : 0f, 0f, 0f, 0f);
                case VolumeParameter<Color> c:
                    Color col = c.value;
                    return new Vector4(col.r, col.g, col.b, col.a);
                case VolumeParameter<Vector2> v2: return new Vector4(v2.value.x, v2.value.y, 0f, 0f);
                case VolumeParameter<Vector3> v3: return new Vector4(v3.value.x, v3.value.y, v3.value.z, 0f);
                case VolumeParameter<Vector4> v4: return v4.value;
                default: throw new NotSupportedException($"Cannot read unsupported VolumeParameter type '{parameter?.GetType().Name ?? "null"}'.");
            }
        }

        public static void Write(VolumeParameter parameter, Vector4 value)
        {
            switch (parameter)
            {
                case VolumeParameter<float> f: f.Override(value.x); break;
                case VolumeParameter<int> i: i.Override(Mathf.RoundToInt(value.x)); break;
                case VolumeParameter<bool> b: b.Override(value.x >= 0.5f); break;
                case VolumeParameter<Color> c: c.Override(new Color(value.x, value.y, value.z, value.w)); break;
                case VolumeParameter<Vector2> v2: v2.Override(new Vector2(value.x, value.y)); break;
                case VolumeParameter<Vector3> v3: v3.Override(new Vector3(value.x, value.y, value.z)); break;
                case VolumeParameter<Vector4> v4: v4.Override(value); break;
                default: throw new NotSupportedException($"Cannot write unsupported VolumeParameter type '{parameter?.GetType().Name ?? "null"}'.");
            }
        }
    }
}
