using Microsoft.CodeAnalysis;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Celerio.Analyzers.Generators;

public static class DefaultPropProvider
{
    //TODO: Write more Providers
    //TODO: Fix barely-working GenerateDeserializerForType
    
    public static PropProvider PathProvider = 
        new ((x)=>true, 0, 1, 
            (symbol, sb, tab, next) =>
            {
                var t = Tabs.Tab(tab);
                sb.AppendLine($"{t}{symbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} parameter_{symbol.Name} = {PropProviderUtils.GenerateDeserializerForType(symbol.Type, $"path_{symbol.Name}")};");
                if(next != null)
                    next(sb, tab);
            });
    
    public static PropProvider QueryProvider = 
        new ((x)=>true, int.MaxValue, 1, 
            (symbol, sb, tab, next) =>
            {
                var t = Tabs.Tab(tab);
                sb.AppendLine($"{t}{symbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} parameter_{symbol.Name} = {PropProviderUtils.GenerateDeserializerForType(symbol.Type, $"request.Query[\"{symbol.Name}\"]")};");
                if(next != null)
                    next(sb, tab);
            });
}