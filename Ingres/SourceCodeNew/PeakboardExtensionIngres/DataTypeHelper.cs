using System;

namespace PeakboardExtensionIngres
{
    static class DataTypeHelper
    {
        public static bool IsNumericType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        public static object GetOrConvertNumericTypeToDouble(Type type, object value)
        {
            if (value == null || Convert.IsDBNull(value))
            {
                if (type == typeof(string))
                    return string.Empty;
                if (type == typeof(bool))
                    return false;
                if (DataTypeHelper.IsNumericType(type))
                    return 0d;

                return string.Empty;
            }

            if (type == typeof(string))
                return value is string ? value : value.ToString();
            if (type == typeof(bool))
                return value is bool ? value : false;
            if (DataTypeHelper.IsNumericType(type))
                return value is double ? value : Convert.ToDouble(value);

            return (value ?? string.Empty).ToString();
        }
    }
}