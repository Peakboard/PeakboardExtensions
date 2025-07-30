using System;

namespace MySql
{
    internal static class DataTypeHelper
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
                if (IsNumericType(type))
                    return 0d;

                return string.Empty;
            }

            try
            {
                if (type == typeof(string))
                    return value.ToString();
                if (type == typeof(bool))
                    return Convert.ToBoolean(value);
                if (IsNumericType(type))
                    return Convert.ToDouble(value);
                if (type == typeof(DateTime))
                    return Convert.ToDateTime(value);

                return value.ToString();
            }
            catch
            {
                return GetDefaultValue(type);
            }
        }

        public static object GetDefaultValue(Type type)
        {
            if (type == typeof(string))
                return string.Empty;
            if (type == typeof(bool))
                return false;
            if (IsNumericType(type))
                return 0d;
            if (type == typeof(DateTime))
                return DateTime.MinValue;

            return string.Empty;
        }
    }
}