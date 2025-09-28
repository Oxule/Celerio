using Microsoft.CodeAnalysis;

namespace Celerio.Analyzers.Generators.Validators;

public static class DefaultPropValidators
{
    public static PropProvider StringLength = PropValidatorHelpers.CreateAttributeBasedValidator(
        "global::System.ComponentModel.DataAnnotations.LengthAttribute",
        x => x.Type.SpecialType == SpecialType.System_String,
        (symbol, attr) =>
        {
            var min = attr.ConstructorArguments[0].Value as int?;
            var max = attr.ConstructorArguments[1].Value as int?;
            return $"{min} <= parameter_{symbol.Name}.Length && parameter_{symbol.Name}.Length <= {max}";
        },
        (symbol, attr) =>
        {
            var min = attr.ConstructorArguments[0].Value as int?;
            var max = attr.ConstructorArguments[1].Value as int?;
            return $"\"Incorrect parameter '{symbol.Name}' length [{min},{max}]\"";
        });

    public static PropProvider StringMinLength = PropValidatorHelpers.CreateAttributeBasedValidator(
        "global::System.ComponentModel.DataAnnotations.MinLengthAttribute",
        x => x.Type.SpecialType == SpecialType.System_String,
        (symbol, attr) =>
        {
            var length = attr.ConstructorArguments[0].Value as int?;
            return $"{length} <= parameter_{symbol.Name}.Length";
        },
        (symbol, attr) =>
        {
            var length = attr.ConstructorArguments[0].Value as int?;
            return $"\"Parameter '{symbol.Name}' length must be at least {length}\"";
        });

    public static PropProvider StringMaxLength = PropValidatorHelpers.CreateAttributeBasedValidator(
        "global::System.ComponentModel.DataAnnotations.MaxLengthAttribute",
        x => x.Type.SpecialType == SpecialType.System_String,
        (symbol, attr) =>
        {
            var length = attr.ConstructorArguments[0].Value as int?;
            return $"parameter_{symbol.Name}.Length <= {length}";
        },
        (symbol, attr) =>
        {
            var length = attr.ConstructorArguments[0].Value as int?;
            return $"\"Parameter '{symbol.Name}' length must not exceed {length}\"";
        });

    public static PropProvider IntRange = CreateNumericRangeValidator(SpecialType.System_Int32, "integer");
    public static PropProvider LongRange = CreateNumericRangeValidator(SpecialType.System_Int64, "long integer");
    public static PropProvider ShortRange = CreateNumericRangeValidator(SpecialType.System_Int16, "short integer");
    public static PropProvider ByteRange = CreateNumericRangeValidator(SpecialType.System_Byte, "byte");
    public static PropProvider SByteRange = CreateNumericRangeValidator(SpecialType.System_SByte, "signed byte");
    public static PropProvider UIntRange = CreateNumericRangeValidator(SpecialType.System_UInt32, "unsigned integer");
    public static PropProvider ULongRange = CreateNumericRangeValidator(SpecialType.System_UInt64, "unsigned long integer");
    public static PropProvider UShortRange = CreateNumericRangeValidator(SpecialType.System_UInt16, "unsigned short integer");
    public static PropProvider FloatRange = CreateNumericRangeValidator(SpecialType.System_Single, "single-precision float");
    public static PropProvider DoubleRange = CreateNumericRangeValidator(SpecialType.System_Double, "double-precision float");

    private static PropProvider CreateNumericRangeValidator(SpecialType specialType, string typeName)
    {
        return PropValidatorHelpers.CreateAttributeBasedValidator(
            "global::System.ComponentModel.DataAnnotations.RangeAttribute",
            x => x.Type.SpecialType == specialType,
            (symbol, attr) =>
            {
                var min = attr.ConstructorArguments[0].Value;
                var max = attr.ConstructorArguments[1].Value;
                return $"{min} <= parameter_{symbol.Name} && parameter_{symbol.Name} <= {max}";
            },
            (symbol, attr) =>
            {
                var min = attr.ConstructorArguments[0].Value;
                var max = attr.ConstructorArguments[1].Value;
                return $"\"Parameter '{symbol.Name}' ({typeName}) must be in range [{min}, {max}]\"";
            });
    }

    public static PropProvider StringRegex = PropValidatorHelpers.CreateAttributeBasedValidator(
        "global::System.ComponentModel.DataAnnotations.RegularExpressionAttribute",
        x => x.Type.SpecialType == SpecialType.System_String,
        (symbol, attr) =>
        {
            var pattern = attr.ConstructorArguments[0].Value as string ?? "";
            return $"Regex.IsMatch(parameter_{symbol.Name}, @\"{pattern.Replace("\"", "\\\"")}\")";
        },
        (symbol, attr) =>
        {
            return $"\"Parameter '{symbol.Name}' does not match the required pattern\"";
        });

    public static PropProvider StringEmail = PropValidatorHelpers.CreateAttributeBasedValidator(
        "global::System.ComponentModel.DataAnnotations.EmailAddressAttribute",
        x => x.Type.SpecialType == SpecialType.System_String,
        (symbol, attr) =>
        {
            var pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            return $"Regex.IsMatch(parameter_{symbol.Name}, @\"{pattern}\")";
        },
        (symbol, attr) =>
        {
            return $"\"Parameter '{symbol.Name}' must be a valid email address\"";
        });

    public static PropProvider StringUrl = PropValidatorHelpers.CreateAttributeBasedValidator(
        "global::System.ComponentModel.DataAnnotations.UrlAttribute",
        x => x.Type.SpecialType == SpecialType.System_String,
        (symbol, attr) =>
        {
            var pattern = @"^https?://[^\s/$.?#].[^\s]*$";
            return $"Regex.IsMatch(parameter_{symbol.Name}, @\"{pattern}\")";
        },
        (symbol, attr) =>
        {
            return $"\"Parameter '{symbol.Name}' must be a valid URL\"";
        });

    public static PropProvider Validatable = new (
        x => x.Type.AllInterfaces.Any(i => i.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::Celerio.IValidatable"),
        0,
        1,
        (symbol, sb, tab, next) =>
        {
            var t = Tabs.Tab(tab);
            sb.AppendLine($"{t}if (parameter_{symbol.Name}.Validate(out string? {symbol.Name}_reason)) {{");
            if (next != null)
                next(sb, tab + 1);
            sb.AppendLine($"{t}}} else {{");
            sb.AppendLine($"{t}\treturn new Result(400).Text({symbol.Name}_reason);");
            sb.AppendLine($"{t}}}");
        });
}
