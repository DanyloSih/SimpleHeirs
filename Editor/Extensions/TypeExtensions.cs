using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SimpleHeirs.Extensions
{
    public static class TypeExtensions
    {
        public static bool HasAttribute<T>(this Type type) where T : Attribute
        {
            return type.GetCustomAttributes(typeof(T), true).Any();
        }

        public static IEnumerable<FieldInfo> GetAvaliableInInspectorFields(this Type type)
        {
            var fields = type.GetAllFields();

            for (int i = fields.Count - 1; i >= 0; i--)
            {
                var field = fields[i];

                // Ignore fields marked with [HideInInspector]
                if (field.GetCustomAttribute<HideInInspector>() != null)
                {
                    fields.RemoveAt(i);
                    continue;
                }

                // Ignore private fields that are not marked with [SerializeField]
                if (!field.IsPublic && field.GetCustomAttribute<SerializeField>() == null)
                {
                    fields.RemoveAt(i);
                    continue;
                }

                // Ignore fields with [Obsolete] attribute
                if (field.GetCustomAttribute<System.ObsoleteAttribute>() != null)
                {
                    fields.RemoveAt(i);
                    continue;
                }

                // Ignore fields marked with [System.NonSerialized] or [NonSerialized]
                if (field.GetCustomAttribute<System.NonSerializedAttribute>() != null)
                {
                    fields.RemoveAt(i);
                    continue;
                }
            }

            return fields;
        }

        public static List<FieldInfo> GetAllFields(
            this Type type)
        {
            const BindingFlags bindingFlags 
                = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            List<FieldInfo> fields = new List<FieldInfo>();
            Type currentType = type;
            List<Type> types = new List<Type>();
            types.Insert(0, currentType);
            while (currentType.BaseType != null)
            {
                currentType = currentType.BaseType;
                types.Insert(0, currentType);
            }

            foreach (var t in types)
            {
                fields.AddRange(t.GetFields(bindingFlags)
                    .Where(a => !fields.Any(b => b.Name == a.Name)));
            }
            return fields;
        }

        public static bool IsUnityPrimitiveType(this Type type)
        {
            return type.IsPrimitive ||
                   type == typeof(string) ||
                   type == typeof(Vector2) ||
                   type == typeof(Vector3) ||
                   type == typeof(Vector4) ||
                   type == typeof(Quaternion) ||
                   type == typeof(Color) ||
                   type == typeof(Color32) ||
                   type == typeof(Matrix4x4) ||
                   type == typeof(Rect) ||
                   type == typeof(Bounds) ||
                   type == typeof(AnimationCurve) ||
                   type == typeof(Gradient) ||
                   type == typeof(LayerMask) ||
                   type.IsSubclassOf(typeof(ICollection)) ||
                   type.IsSubclassOf(typeof(GameObject)) ||
                   type.IsSubclassOf(typeof(Component)) ||
                   type.IsSubclassOf(typeof(UnityEngine.Object)) ||
                   type.IsEnum;
        }
    }
}
