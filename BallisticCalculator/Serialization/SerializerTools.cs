using Gehtsoft.Measurements;
using System;

namespace BallisticCalculator.Serialization
{
    internal static class SerializerTools
    {
        public static Type RemoveNullabilityFromType(Type type)
        {
            var type1 = Nullable.GetUnderlyingType(type);
            if (type1 != null && type1 != type)
                type = type1;
            return type;
        }

        public static bool IsTypeMeasurement(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Measurement<>);
    }
}
