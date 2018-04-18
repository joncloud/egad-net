using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Egad
{
    struct TypeCache
    {
        static ConcurrentDictionary<string, Type> _types
            = new ConcurrentDictionary<string, Type>();

        public static Type GetDataType(string TypeName) =>
            _types.GetOrAdd(TypeName, LoadDataType);

        static Type LoadDataType(string fullName)
        {
            var type = Type.GetType(fullName);
            if (type != null) return type;

            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(asm => asm.GetType(fullName))
                .Where(t => t != null)
                .FirstOrDefault();
        }
    }
}
