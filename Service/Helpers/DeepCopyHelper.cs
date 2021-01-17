using System;
using System.Linq;
using System.Collections.Generic;

namespace ADCCure.BlogFeederService
{
public static class DeepCopyHelper
	{
		
        public static bool IsCollectionGenericTypeEqual(Type type1, Type type2)
        {
            var isCol1 = type1.IsCollection(out Type foundGeneric1);
            if (!isCol1)
            { return false; }
            var isCol2 = type2.IsCollection(out Type foundGeneric2);
            if (!isCol2)
            {
                return false;
            }
            if (foundGeneric2 == foundGeneric1)
            {
                return true;
            }
            return false;
        }
        public static bool IsCollection(this Type type, out Type elementType)
        {
            elementType = type ?? throw new ArgumentNullException(nameof(type));

            // see if this type should be ignored.
            if (type == typeof(string))
            {
                return false;
            }

            Type collectionInterface
                = type.GetInterfaces()
                    .Union(new[] { type })
                    .FirstOrDefault(
                        t => t.IsGenericType
                             && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if (collectionInterface != null)
            {
                elementType = collectionInterface.GetGenericArguments()[0];
                return true;
            }

            return false;
        }
    }
}