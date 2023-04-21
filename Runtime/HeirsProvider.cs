using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SimpleHeirs
{
    public static class HeirsProviderExtensions
    {
        public static T GetValue<T>(this HeirsProvider<T> provider)
            where T : class
        {
            return provider == null ? null : provider.Value;
        }
    }

    [Serializable]
    public class HeirsProvider<T> : BaseHeirsProvider, ISerializationCallbackReceiver
        where T : class
    {
        [HideInInspector, SerializeField] private int _selectedIndex;
        [HideInInspector,SerializeReference, SerializeField] private object _heirObject;
        [HideInInspector, SerializeField] private UnityEngine.Object _heirUnityObject;
        /// <summary>
        /// The object from which the serialization chain started.
        /// Can be filled from PropertyDrawer.
        /// </summary>
        [HideInInspector, SerializeField] private UnityEngine.Object _targetObject;
        [HideInInspector, SerializeField] private bool _isFolded = false;

        [NonSerialized] private bool _isChecked = false;

        public static string SelectedIndexName => nameof(_selectedIndex);
        public static string HeirObjectName => nameof(_heirObject);  
        public static string HeirUnityObjectName => nameof(_heirUnityObject);  
        public static string TargetObjectName => nameof(_targetObject);
        public static string IsFoldedName => nameof(_isFolded);

        /// <param name="heirObject">The new value of the class that inherits from T</param>
        /// <param name="targetObject">The object from which the serialization chain started.</param>
        public HeirsProvider(T heirObject, UnityEngine.Object targetObject)
        {
            _targetObject = targetObject;
            if (heirObject == null)
            {
                return;
            }
            if (typeof(UnityEngine.Object).IsAssignableFrom(heirObject.GetType()))
            {
                _heirUnityObject = (UnityEngine.Object)(object)heirObject;
            }
            else
            {
                _heirObject = heirObject;
            }
        }

        internal T Value
        {
            get
            {
                if (_heirUnityObject != null)
                {
                    return (T)(object)_heirUnityObject;
                }
                if (_heirObject != null)
                {
                    return (T)_heirObject;
                }

                return null;
            }
        }

        public override Type GetBaseType()
        {
            return typeof(T);
        }

        public override Type GetContainedType()
        {
            return Value == null ? typeof(Empty) : typeof(T);
        }

        public override object GetHeirObject() => Value;

        public override object GetClearClone()
        {
            return new HeirsProvider<T>(null, _targetObject);
        }

        public void OnBeforeSerialize()
        {
            if (!_isChecked)
            {
                FixManagedReferencesIssue();
            }
        }

        public void OnAfterDeserialize()
        {
            _isChecked = false;
        }

        private void FixManagedReferencesIssue()
        {
#if UNITY_EDITOR
            if (_targetObject == null)
            {
                return;
            }

            _isChecked = true;
            if (SerializationUtility.HasManagedReferencesWithMissingTypes(_targetObject))
            {
                _heirObject = null;
                _heirUnityObject = null;
                _selectedIndex = 0;
                SerializationUtility.ClearAllManagedReferencesWithMissingTypes(_targetObject);
            }
#endif
        }
    }
}
