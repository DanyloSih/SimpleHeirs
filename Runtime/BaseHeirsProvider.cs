using System;
using UnityEditor;

namespace SimpleHeirs
{
    /// <summary>
    /// Created as a marker to define the object to be drawn.
    /// </summary>
    [Serializable]
    public abstract class BaseHeirsProvider
    {
        public abstract object GetClearClone();

        public abstract object GetHeirObject();

        public abstract Type GetContainedType();

        public abstract Type GetBaseType();
    }
}
