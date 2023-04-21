using System;
using System.Collections.Generic;
using SimpleHeirs.Extensions;
using UnityEditor;

namespace SimpleHeirs.Editor
{
    [Serializable]
    public class PropertyInfoContainer
    {
        private List<PropertyInfo> _propertiesInfoCash = new List<PropertyInfo>();

        public PropertyInfoContainer()
        {
            _propertiesInfoCash = new List<PropertyInfo>();
        }

        public void ClearInfoCash()
        {
            _propertiesInfoCash.Clear();
        }

        public void RemoveInfo(PropertyInfo propertyInfo)
        {
            _propertiesInfoCash.Remove(propertyInfo);
        }

        public PropertyInfo GetInfo(SerializedProperty serializedProperty)
        {
            if (_propertiesInfoCash.Exists(x => x.Property.IsSame(serializedProperty)))
            {
                return _propertiesInfoCash.Find(x => x.Property.IsSame(serializedProperty));
            }
            else
            {
                var newInfo = new PropertyInfo(serializedProperty);
                _propertiesInfoCash.Add(newInfo);
                return newInfo;
            }
        }

        public PropertyInfo UpdateInfo(SerializedProperty serializedProperty)
        {
            int index = _propertiesInfoCash.FindIndex(x => x.Property.IsSame(serializedProperty));
            if (index > -1)
            {
                _propertiesInfoCash.RemoveAt(index);
            }

            var newInfo = new PropertyInfo(serializedProperty);
            _propertiesInfoCash.Add(newInfo);
            return newInfo;
        }
    }
}
