using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SimpleHeirs.Extensions
{
    public static class SerializedPropertyExtensions
    {
        private static FieldInfo s_LastField;

        public static SerializedProperty GetParentWithoutIndex(this SerializedProperty property)
        {
            return property.serializedObject.FindProperty(GetPreviousPropertyPath(property.propertyPath, false));
        }

        public static SerializedProperty GetParent(this SerializedProperty property)
        {
            string path = property.propertyPath;
            int lastIndex = path.LastIndexOf(".");
            if (lastIndex >= 0)
            {
                string parentPath = path.Substring(0, lastIndex);
                return property.serializedObject.FindProperty(parentPath);
            }
            return null;
        }

        public static bool IsSame(this SerializedProperty prop1, SerializedProperty prop2)
        {
            try
            {
                return prop1.propertyPath.Equals(prop2.propertyPath);
            }
            catch { return false; }
        }

        public static object TraversePropertyPath(this SerializedProperty property, object value, Func<FieldInfo, object, object, string[], object> valueAccessor)
        {
            string[] paths = property.propertyPath.Replace(".Array.data[", "[").Split('.');

            string[] collection = paths[0].Replace("]", "").Split($"[");
            object valueObj = property.serializedObject.targetObject;
            object previousObj = valueObj;
            FieldInfo field = valueObj.GetType().GetAllFields().First(x => x.Name == collection[0]);
            valueObj = GetValue(field, previousObj, value, collection);
            for (int i = 1; i < paths.Length; i++)
            {
                previousObj = valueObj;
                Type t = valueObj == null ? field.FieldType : valueObj.GetType();
                try
                {
                    valueObj = GetValueObj(value, t, paths[i], previousObj, out collection, ref field);
                }
                catch (InvalidOperationException)
                {
                    s_LastField = field;
                    return null;
                }
            }
            s_LastField = field;
            return valueAccessor(field, previousObj, value, collection);
        }

        public static void LogFields(this SerializedProperty property)
        {
            StringBuilder result = new StringBuilder();
            var iterator = property.Copy();
            var endProperty = iterator.GetEndProperty();
            while (iterator.NextVisible(true) && !SerializedProperty.EqualContents(iterator, endProperty))
            {
                result.Append($"{iterator.propertyPath} ({iterator.type}) \n");
            }
            Debug.Log($"Fields for {property.propertyPath}: \n{result}");
        }

        public static int[] GetDimensionsIDs(this SerializedProperty property)
        {
            var dimensions = property.propertyPath.Replace(".Array.data[", "[").Split('[', ']')
                .Select(x => int.TryParse(x, out int v) ? v : -1).Where(x => x != -1).ToArray();

            
            if(dimensions.Length == 0) 
            {
                return new int[] { 0 };
            }
            else
            {
                return dimensions;
            }
        }

        public static FieldInfo GetField(this SerializedProperty property)
        {
            property.TraversePropertyPath(null, GetValue);
            return s_LastField;
        }

        public static Type GetDataType(this SerializedProperty property)
        {
            object valueObj = property.GetDataValue();
            return valueObj == null ? s_LastField.FieldType : valueObj.GetType();
        }

        public static object GetDataValue(this SerializedProperty property)
        {
            var pt = property.propertyType;
            if (pt == SerializedPropertyType.ObjectReference || pt == SerializedPropertyType.ExposedReference)
            {
                return property.objectReferenceValue;
            }
            else if (pt == SerializedPropertyType.ManagedReference)
            {
                return property.managedReferenceValue;
            }

            return property.TraversePropertyPath(null, GetValue);
        }

        public static void SetDataValue(this SerializedProperty property, object value)
        {
            var pt = property.propertyType;
            if (pt == SerializedPropertyType.ObjectReference || pt == SerializedPropertyType.ExposedReference)
            {
                property.objectReferenceValue = (UnityEngine.Object)value;
            }
            else if (pt == SerializedPropertyType.ManagedReference)
            {
                property.managedReferenceValue = value;
            }
            else
            {
                object valueObj = property.TraversePropertyPath(value, SetValue);
            }
        }

        private static object GetValue(FieldInfo field, object previousObj, object value, string[] collection)
        {
            return previousObj == null ? null :
                collection.Length > 1
                ? ((IList)field.GetValue(previousObj))[int.Parse(collection[1])]
                : field.GetValue(previousObj);
        }

        private static object SetValue(FieldInfo field, object previousObj, object value, string[] collection)
        {
            if (collection.Length > 1)
            {
                ((IList)field.GetValue(previousObj))[int.Parse(collection[1])] = value;
            }
            else
            {
                field.SetValue(previousObj, value);
            }
            return value;
        }

        private static object GetValueObj(object value, Type type, string path, object previousObj, out string[] collection, ref FieldInfo field)
        {
            var newCollection = path.Replace("]", "").Split($"[");
            collection = newCollection;
            if (previousObj == null && typeof(BaseHeirsProvider).IsAssignableFrom(field.DeclaringType))
            {
                type = field.DeclaringType.GenericTypeArguments[0];
            }
            
            field = type.GetAllFields().First(x => x.Name == newCollection[0]);

            return GetValue(field, previousObj, value, collection);
        }

        private static string GetPreviousPropertyPath(string propertyPath, bool includeIndex = true)
        {
            string[] path = propertyPath.Replace(".Array.data[", "[").Split('.');
            StringBuilder result = new StringBuilder();
            if (path.Length == 0)
            {
                return "";
            }

            if (path.Length == 1)
            {
                return path[0].Replace("[", ".Array.data[");
            }
            else
            {
                for (int i = 0; i < path.Length - 2; i++)
                {
                    result.Append(path[i].Replace("[", ".Array.data["));
                }
                string last = path[path.Length - 2];
                if (!includeIndex)
                {
                    last = last.Split('[')[0];
                }
                result.Append(last.Replace("[", ".Array.data["));
                return result.ToString();
            }
        }
    }
}
