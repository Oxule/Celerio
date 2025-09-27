using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Celerio.Analyzers.Generators.Validators;

public static class PropValidatorHelpers
{
    public static PropProvider CreateAttributeBasedValidator(
        string attributeFqName,
        Predicate<IParameterSymbol> typePredicate,
        Func<IParameterSymbol, AttributeData, string> conditionBuilder,
        Func<IParameterSymbol, AttributeData, string> errorMessageBuilder)
    {
        Predicate<IParameterSymbol> appliesPredicate = x =>
            typePredicate(x) &&
            x.GetAttributes().Any(a => a.AttributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == attributeFqName);

        Func<IParameterSymbol, AttributeData> attributeGetter = x =>
            x.GetAttributes().First(a => a.AttributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == attributeFqName);

        return new PropProvider(
            appliesPredicate,
            0,
            1,
            (symbol, sb, tab, next) =>
            {
                var t = Tabs.Tab(tab);
                var attribute = attributeGetter(symbol);
                var condition = conditionBuilder(symbol, attribute);
                var errorMessage = errorMessageBuilder(symbol, attribute);

                sb.AppendLine($"{t}if ({condition}) {{");
                if (next != null)
                    next(sb, tab + 1);
                sb.AppendLine($"{t}}} else {{");
                sb.AppendLine($"{t}\treturn new Result(400).Text({errorMessage});");
                sb.AppendLine($"{t}}}");
            });
    }
}
