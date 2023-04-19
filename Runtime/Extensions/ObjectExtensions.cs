using System.Collections.Generic;
using System.Reflection;

namespace SimpleHeirs.Extensions
{
    public static class ObjectExtensions
    {
        public static IEnumerable<FieldInfo> GetAvaliableInInspectorFields(this object obj)
        {
            var type = obj.GetType();

            return type.GetAvaliableInInspectorFields();
        }
    }
}
