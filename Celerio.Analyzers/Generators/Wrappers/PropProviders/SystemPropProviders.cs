using Microsoft.CodeAnalysis;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Celerio.Analyzers.Generators;

public static class SystemPropProviders
{
    public static PropProvider RequestProvider = 
        new ((x)=>x.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::Celerio.Request", 0, 0, 
            (symbol, sb, tab, next) =>
            {
                var t = Tabs.Tab(tab);
                sb.AppendLine($"{t}{symbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} parameter_{symbol.Name} = request;");
                if(next != null)
                    next(sb, tab);
            });
}