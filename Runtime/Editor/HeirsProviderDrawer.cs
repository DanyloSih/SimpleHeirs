using UnityEditor;
using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using SimpleHeirs.Extensions;

namespace SimpleHeirs.Editor
{
    [CustomPropertyDrawer(typeof(HeirsProvider<>))]
    public class HeirsProviderDrawer : PropertyDrawer
    {
        private static readonly Color _darkBackgroundColor = new Color(0.19f, 0.19f, 0.19f);
        private static readonly Color _lightBackgroundColor = new Color(0.3f, 0.3f, 0.3f);
        private static readonly int _leftStep = 15;
        private static readonly int _rightStep = 5;
        private static readonly int _padding = 4;

        private object _previousPropertyData = null;
        private string _previousPropertyPath = "";
        private Rect _position;

        public bool IsInitialized = false;
        public List<Type> SubTypes = new List<Type>();
        public string[] FullNames = new string[0];
        public Rect CurrentRect;
        public SerializedProperty HeirObjectProperty;
        public SerializedProperty HeirUnityObjectProperty;
        public SerializedProperty SelectedIndexProperty;
        public SerializedProperty TargetObjectProperty;
        private SerializedProperty IsFoldedProperty;
        public PropertyInfoContainer PropertiesCash;
        public int[] Path;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!InitializeCash(property, ref position, property.GetDimensionsIDs()))
            {
                return;
            }
            
            _position = position;
            var propRect = new Rect(CurrentRect);

            CurrentRect.height += DrawSubclassesHeader(ref propRect, label, _leftStep, _rightStep);

            int selectedIndex = SelectedIndexProperty.intValue;
            if (selectedIndex <= 0 || !IsFoldedProperty.boolValue)
            {
                return;
            }

            label.text = $"";
            if (typeof(UnityEngine.Object).IsAssignableFrom(SubTypes[selectedIndex]))
            {
                HeirObjectProperty.managedReferenceValue = null;
                CurrentRect.height
                    += DrawUnityObjectField(HeirUnityObjectProperty, ref propRect, label, SubTypes[selectedIndex]);
            }
            else
            {
                HeirUnityObjectProperty.objectReferenceValue = null;
                CurrentRect.height
                    += DrawObjectProperties(HeirObjectProperty, ref propRect, label, _leftStep, _rightStep, 0);
            }
            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = GetHeaderHeight();
            IsFoldedProperty = property.FindPropertyRelative(HeirsProvider<object>.IsFoldedName);
            if (!IsFoldedProperty.boolValue
            || !InitializeCash(property, ref _position, property.GetDimensionsIDs()))
            {
                return height;
            }
            Type selectedType = SubTypes[SelectedIndexProperty.intValue];
            if (!typeof(UnityEngine.Object).IsAssignableFrom(selectedType))
            {
                var info = PropertiesCash.GetInfo(HeirObjectProperty);
                height += GetIteratedRect(info.Subproperties, ref CurrentRect, label).height;
            }
            else
            {
                height += GetUnityObjectFieldHeight();
            }
            return height;
        }

        private bool InitializeCash(SerializedProperty property, ref Rect position, int[] path)
        {
            Path = path;
            HeirObjectProperty = property.FindPropertyRelative(HeirsProvider<object>.HeirObjectName);
            HeirUnityObjectProperty = property.FindPropertyRelative(HeirsProvider<object>.HeirUnityObjectName);
            SelectedIndexProperty = property.FindPropertyRelative(HeirsProvider<object>.SelectedIndexName);
            TargetObjectProperty = property.FindPropertyRelative(HeirsProvider<object>.TargetObjectName);
            IsFoldedProperty = property.FindPropertyRelative(HeirsProvider<object>.IsFoldedName);

            BaseHeirsProvider heirsProvider = null;
            CurrentRect = new Rect(position.x, position.y, position.width, 0);
            if (!IsInitialized)
            {
                PropertiesCash = new PropertyInfoContainer();
                
                var localPropertyInfo = PropertiesCash.GetInfo(property);
                heirsProvider = localPropertyInfo.DataValue as BaseHeirsProvider;
                if (heirsProvider == null)
                {
                    return false;
                }

                SubTypes = GetSubtypes(heirsProvider.GetBaseType());
                FullNames = SubTypes.Select(x => x.Name).ToArray();
                IsInitialized = true;
            }

            PropertyInfo serializedPropertyValuePropertyInfo
                    = PropertiesCash.GetInfo(TargetObjectProperty);
            if(serializedPropertyValuePropertyInfo.DataValue == null) 
            {
                TargetObjectProperty.SetDataValue(property.serializedObject.targetObject);
                property.serializedObject.ApplyModifiedProperties();
            }

            PropertyInfo propertyInfo = PropertiesCash.GetInfo(property);
            
            object tmpDataValue = null;
            if (propertyInfo.DataValue != null)
            {
                heirsProvider = (BaseHeirsProvider)propertyInfo.DataValue;
                tmpDataValue = heirsProvider.GetHeirObject();
            }
            if (tmpDataValue != null
             && tmpDataValue == _previousPropertyData
             && property.propertyPath != _previousPropertyPath
             && Path[0] != 0)
            {
                property.SetDataValue(heirsProvider.GetClearClone());
            }

            _previousPropertyData = tmpDataValue;
            _previousPropertyPath = property.propertyPath;

            return true;
        }

        private float DrawUnityObjectField(
            SerializedProperty property,
            ref Rect position,
            GUIContent label,
            Type fieldType)
        {
            float height = GetUnityObjectFieldHeight();
            if (property.objectReferenceValue != null &&
                !property.objectReferenceValue.GetType().IsAssignableFrom(fieldType))
            {
                property.objectReferenceValue = null;
            }
            property.objectReferenceValue = EditorGUI.ObjectField(
                GetNextRect(ref position, EditorGUIUtility.singleLineHeight, _rightStep, _rightStep, _padding),
                label, property.objectReferenceValue, fieldType, true);

            return height;
        }

        private float GetUnityObjectFieldHeight()
        {
            return EditorGUIUtility.singleLineHeight + _padding;
        }

        private float DrawObjectProperties(SerializedProperty property, ref Rect position, GUIContent label, float leftStep, float rightStep, int id)
        {
            float height = 0;
            var propertyInfo = PropertiesCash.GetInfo(property);

            var rect = GetIteratedRect(propertyInfo.Subproperties, ref position, label, leftStep - _leftStep, rightStep - _rightStep);
            rect.yMax += _padding / 2;
            EditorGUI.DrawRect(rect, id % 2 == 0 ? _darkBackgroundColor : _lightBackgroundColor);

            foreach (var subproperty in propertyInfo.Subproperties)
            {
                label.text = subproperty.displayName;
                var subpropertyInfo = PropertiesCash.GetInfo(subproperty);
                if (!subpropertyInfo.IsSubclassesDrawer && !subpropertyInfo.IsUnityPrimitive && subpropertyInfo.SubFields.Count() > 0)
                {
                    var fieldRect = GetNextRect(ref position, EditorGUIUtility.singleLineHeight, leftStep, rightStep, _padding);
                    EditorGUI.DrawRect(fieldRect, id % 2 != 0 ? _darkBackgroundColor : _lightBackgroundColor);
                    EditorGUI.LabelField(fieldRect, label);
                    height += EditorGUIUtility.singleLineHeight + _padding;
                    height += DrawObjectProperties(subproperty, ref position, label, leftStep + _leftStep, rightStep + _rightStep, id + 1);
                }
                else
                {
                    var localHeight = EditorGUI.GetPropertyHeight(subproperty, label);
                    try
                    {
                        EditorGUI.PropertyField(GetNextRect(ref position, localHeight, leftStep, rightStep, _padding), subproperty, label);
                        localHeight = EditorGUI.GetPropertyHeight(subproperty, label);
                        height += localHeight + _padding;
                    }
                    catch
                    {
                        localHeight = EditorGUI.GetPropertyHeight(subproperty, label);
                        height += localHeight + _padding;
                        return height;
                    }
                }
            }

            return height;
        }

        private float DrawSubclassesHeader(ref Rect position, GUIContent label, float leftStep, float rightStep)
        {
            float height = GetHeaderHeight();
            int selectedIndex = SelectedIndexProperty.intValue;
            bool isFolded = IsFoldedProperty.boolValue;

            EditorGUI.BeginChangeCheck();
            var headerRect = GetNextRect(
                ref position, height, leftStep - _leftStep, rightStep - _rightStep);

            if (selectedIndex > 0)
            {
                isFolded = EditorGUI.Foldout(headerRect, isFolded, label);
                label.text = "";
                headerRect.xMin += 75;
            }
            
            selectedIndex = EditorGUI.Popup(headerRect, label.text, selectedIndex, FullNames);

            position.y += position.height;
            position.height = 0;

            if (EditorGUI.EndChangeCheck())
            {
                PropertiesCash.ClearInfoCash();
                SelectedIndexProperty.intValue = selectedIndex;
                IsFoldedProperty.boolValue = isFolded;

                if (selectedIndex <= 0)
                {
                    HeirObjectProperty.managedReferenceValue = null;
                    HeirUnityObjectProperty.objectReferenceValue = null;

                    return height;
                }

                Type selectedType = SubTypes[selectedIndex];
                if (!typeof(UnityEngine.Object).IsAssignableFrom(selectedType))
                {
                    object oldObject = HeirObjectProperty.managedReferenceValue;
                    object newObject = Activator.CreateInstance(selectedType);

                    if (oldObject != null && oldObject.GetType() == selectedType)
                    {
                        EditorJsonUtility.FromJsonOverwrite(JsonUtility.ToJson(oldObject), newObject);
                    }
                    HeirObjectProperty.managedReferenceValue = newObject;
                    HeirObjectProperty.SetDataValue(newObject);
                    HeirObjectProperty.serializedObject.ApplyModifiedProperties();
                }
            }

            return height;
        }

        private static float GetHeaderHeight()
        {
            return EditorGUIUtility.singleLineHeight + _padding / 2;
        }

        private float GetPropertyHeight(SerializedProperty property, GUIContent label, float leftStep, float rightStep)
        {
            float height = 0;
            var propertyInfo = PropertiesCash.GetInfo(property);

            if (!propertyInfo.IsSubclassesDrawer && !propertyInfo.IsUnityPrimitive && propertyInfo.SubFields.Count() > 0)
            {
                height += EditorGUIUtility.singleLineHeight + _padding;

                foreach (var subproperty in propertyInfo.Subproperties)
                {
                    height += GetPropertyHeight(subproperty, label, rightStep, leftStep);
                }
            }
            else
            {
                height += EditorGUI.GetPropertyHeight(property, label) + _padding;
            }
            
            return height;
        }

        private Rect GetIteratedRect(IEnumerable<SerializedProperty> properties, ref Rect currentRect, GUIContent label, float leftOffset = 0, float rightOffset = 0)
        {
            Rect startRect = new Rect(currentRect);
            if (properties.Count() == 0)
            {
                return startRect;
            }

            currentRect.yMin -= _padding;
            Rect result = GetNextRect(ref currentRect, GetPropertyHeight(properties.ElementAt(0), label, leftOffset, rightOffset));
            for (int i = 1; i < properties.Count(); i++)
            {
                var property = properties.ElementAt(i); 
                result.height += GetNextRect(ref currentRect, GetPropertyHeight(property, label, leftOffset, rightOffset)).height;
            }
            currentRect = startRect;
            return GetRectWithOffsets(result, leftOffset, rightOffset);
        }

        private Rect GetNextRect(ref Rect currentRect, float height, float leftOffset = 0, float rightOffset = 0, float verticalPadding = 0)
        {
            var previousRect = new Rect(currentRect);
            currentRect.position += new Vector2(0, previousRect.height + verticalPadding);
            currentRect.height = height;
            return GetRectWithOffsets(currentRect, leftOffset, rightOffset);
        }

        private static Rect GetRectWithOffsets(Rect original, float leftOffset, float rightOffset)
        {
            var newRect = new Rect(original);
            newRect.width -= rightOffset + leftOffset;
            newRect.x += leftOffset;
            return newRect;
        }

        private static List<Type> GetSubtypes(Type objectType)
        {
            return new Type[] { typeof(Empty) }.Concat(AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => objectType.IsAssignableFrom(p) && !p.IsAbstract)).ToList();                
        }
    }
}
