using System.Globalization;
using Microsoft.CodeAnalysis;

namespace Celerio.Analyzers.Generators;

public static class PropProviderUtils
{
    public static string GenerateDeserializerForType(ITypeSymbol typeSymbol, string getter)
    {
        if (typeSymbol is INamedTypeSymbol named && named.IsGenericType
                                                 && named.OriginalDefinition.ToString() == "System.Nullable<T>")
        {
            var underlying = named.TypeArguments[0];
            var inner = GenerateDeserializerForType(underlying, getter);
            return $"string.IsNullOrEmpty({getter}) ? null : {inner}";
        }

        switch (typeSymbol.SpecialType)
        {
            case SpecialType.System_String:
                return getter;
            case SpecialType.System_Boolean:
                return $"bool.Parse({getter})";
            case SpecialType.System_Char:
                return $"char.Parse({getter})";
            case SpecialType.System_SByte:
                return $"sbyte.Parse({getter}, {typeof(NumberFormatInfo).FullName}.InvariantInfo)";
            case SpecialType.System_Byte:
                return $"byte.Parse({getter}, {typeof(NumberFormatInfo).FullName}.InvariantInfo)";
            case SpecialType.System_Int16:
                return $"short.Parse({getter}, {typeof(NumberFormatInfo).FullName}.InvariantInfo)";
            case SpecialType.System_UInt16:
                return $"ushort.Parse({getter}, {typeof(NumberFormatInfo).FullName}.InvariantInfo)";
            case SpecialType.System_Int32:
                return $"int.Parse({getter}, {typeof(NumberFormatInfo).FullName}.InvariantInfo)";
            case SpecialType.System_UInt32:
                return $"uint.Parse({getter}, {typeof(NumberFormatInfo).FullName}.InvariantInfo)";
            case SpecialType.System_Int64:
                return $"long.Parse({getter}, {typeof(NumberFormatInfo).FullName}.InvariantInfo)";
            case SpecialType.System_UInt64:
                return $"ulong.Parse({getter}, {typeof(NumberFormatInfo).FullName}.InvariantInfo)";
            case SpecialType.System_Single:
                return $"float.Parse({getter}, {typeof(NumberFormatInfo).FullName}.InvariantInfo)";
            case SpecialType.System_Double:
                return $"double.Parse({getter}, {typeof(NumberFormatInfo).FullName}.InvariantInfo)";
            case SpecialType.System_Decimal:
                return $"decimal.Parse({getter}, {typeof(NumberFormatInfo).FullName}.InvariantInfo)";
            case SpecialType.System_Object:
                return $"JsonSerializer.Deserialize<object>({getter})";
        }

        var fullName = typeSymbol.ToString();

        if (fullName == "System.DateTime")
            return $"DateTime.Parse({getter}, {typeof(CultureInfo).FullName}.InvariantCulture)";
        if (fullName == "System.DateTimeOffset")
            return $"DateTimeOffset.Parse({getter}, {typeof(CultureInfo).FullName}.InvariantCulture)";
        if (fullName == "System.TimeSpan")
            return $"TimeSpan.Parse({getter}, {typeof(CultureInfo).FullName}.InvariantCulture)";
        if (fullName == "System.Guid")
            return $"Guid.Parse({getter})";
        if (fullName == "System.Uri")
            return $"new Uri({getter}, UriKind.RelativeOrAbsolute)";
        if (fullName == "System.Byte[]")
            return $"Convert.FromBase64String({getter})";

        if (typeSymbol.TypeKind == TypeKind.Enum)
        {
            return $"({typeSymbol})Enum.Parse(typeof({typeSymbol}), {getter})";
        }

        return $"JsonSerializer.Deserialize<{typeSymbol}>({getter})";
    }
    
    public static string FormatConstant(object? value, string targetTypeName)
    {
        if (value == null) return "null";

        switch (Type.GetTypeCode(value.GetType()))
        {
            case TypeCode.String:
                return $"\"{EscapeString((string)value)}\"";
            case TypeCode.Char:
                return $"'{EscapeChar((char)value)}'";
            case TypeCode.Boolean:
                return value.ToString()!.ToLowerInvariant();
            case TypeCode.SByte:
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
                var s = Convert.ToString(value, CultureInfo.InvariantCulture);
                if (value is long) return s + "L";
                if (value is ulong) return s + "UL";
                return s!;
            case TypeCode.Single:
                return ((float)value).ToString(CultureInfo.InvariantCulture) + "f";
            case TypeCode.Double:
                return ((double)value).ToString(CultureInfo.InvariantCulture) + "d";
            case TypeCode.Decimal:
                return ((decimal)value).ToString(CultureInfo.InvariantCulture) + "m";
            default:
                var t = value.GetType();
                if (t.IsEnum)
                {
                    var underlying = Convert.ToInt64(value).ToString(CultureInfo.InvariantCulture);
                    return $"({targetTypeName}){underlying}";
                }

                return value.ToString() ?? "null";
        }
    }

    public static string EscapeString(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
    }

    public static string EscapeChar(char c)
    {
        switch (c)
        {
            case '\\': return "\\\\";
            case '\'': return "\\'";
            case '\n': return "\\n";
            case '\r': return "\\r";
            case '\t': return "\\t";
            default: return c.ToString();
        }
    }
}