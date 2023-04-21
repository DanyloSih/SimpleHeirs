using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SimpleHeirs.Extensions;
using UnityEditor;

namespace SimpleHeirs.Editor
{
    public struct PropertyInfo
    {
        public SerializedProperty Property { get; private set; }
        public Type DataType { get; private set; }
        public object DataValue { get; private set; }
        public FieldInfo FieldInfo { get; private set; }
        public IEnumerable<SerializedProperty> Subproperties { get; private set; }
        public IEnumerable<FieldInfo> SubFields { get; private set; }
        public bool IsUnityPrimitive { get; private set; }
        public bool IsSubclassesDrawer { get; private set; }

        public PropertyInfo(SerializedProperty property)
        {
            Property = property;
            DataValue = null;

            SerializedPropertyType pt = property.propertyType;
            if (pt == SerializedPropertyType.ObjectReference || pt == SerializedPropertyType.ExposedReference)
            {
                DataValue = property.objectReferenceValue;
            }
            if (pt == SerializedPropertyType.ManagedReference)
            {
                DataValue = property.managedReferenceValue;
            }
            if (DataValue == null)
            {
                DataValue = property.GetDataValue();
            }
            DataType = property.GetDataType();
            FieldInfo = property.GetField();
            IsUnityPrimitive = DataType.IsUnityPrimitiveType();
            IsSubclassesDrawer = typeof(BaseHeirsProvider).IsAssignableFrom(FieldInfo.FieldType);
            SubFields = DataType.GetAvaliableInInspectorFields();
            Subproperties = SubFields.Select(x => property.FindPropertyRelative(x.Name)).Where(x => x != null);
        }

        public override bool Equals(object obj)
        {
            if (obj is PropertyInfo)
            {
                var param = (PropertyInfo)obj;
                return Property.IsSame(param.Property);  
            }
            return  false;
        }

        public override int GetHashCode()
        {
            return Property.propertyPath.GetHashCode();
        }
    }
}
