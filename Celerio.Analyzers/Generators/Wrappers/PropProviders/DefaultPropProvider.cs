using Microsoft.CodeAnalysis;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Celerio.Analyzers.Generators;

public static class DefaultPropProvider
{
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

                var p = $"parameter_{symbol.Name}";
                
                sb.AppendLine($"{t}if(request.Query.TryGetValue(\"{symbol.Name}\", out var query_{symbol.Name})){{");
                sb.AppendLine($"{t}\t{symbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {p} = {PropProviderUtils.GenerateDeserializerForType(symbol.Type, $"query_{symbol.Name}")};");
                if(next != null)
                    next(sb, tab+1);
                sb.AppendLine($"{t}}}");
                if (symbol.HasExplicitDefaultValue)
                {
                    sb.AppendLine($"{t}else {{");
                    sb.AppendLine($"{t}\t{symbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {p} = {PropProviderUtils.FormatConstant(symbol.Type, symbol.ExplicitDefaultValue)};");
                    if(next != null)
                        next(sb, tab+1);
                    sb.AppendLine($"{t}}}");
                }
                else if (symbol.Type.NullableAnnotation == NullableAnnotation.Annotated || symbol.Type.ToDisplayString().StartsWith("System.Nullable<"))
                {
                    sb.AppendLine($"{t}else {{");
                    sb.AppendLine($"{t}\t{symbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {p} = null;");
                    if(next != null)
                        next(sb, tab+1);
                    sb.AppendLine($"{t}}}");
                }
                else
                {
                    sb.AppendLine($"{t}else {{");
                    sb.AppendLine($"{t}\treturn new Result(400).Text(\"Required query '{symbol.Name}' is undefined\");");
                    sb.AppendLine($"{t}}}");
                }
            });
    
    public static PropProvider BodyProvider =
        new ((x)=>x.Name.ToLower() == "body", 0, 5,
            (symbol, sb, tab, next) =>
            {
                var t = Tabs.Tab(tab);
                var p = $"parameter_{symbol.Name}";
                var typeDisplay = symbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var stringConversion = "System.Text.Encoding.UTF8.GetString(request.Body)";
                sb.AppendLine($"{t}if(request.Body.Length > 0){{");

                if(symbol.Type.ToDisplayString() == "byte[]")
                {
                    sb.AppendLine($"{t}\t{typeDisplay} {p} = request.Body;");
                }
                else if(symbol.Type.ToDisplayString() == "string")
                {
                    sb.AppendLine($"{t}\t{typeDisplay} {p} = {stringConversion};");
                }
                else
                {
                    sb.AppendLine($"{t}\t{typeDisplay} {p} = JsonSerializer.Deserialize<{typeDisplay}>({stringConversion});");
                }

                if(next != null)
                    next(sb, tab+1);

                sb.AppendLine($"{t}}}");
                if(symbol.HasExplicitDefaultValue)
                {
                    sb.AppendLine($"{t}else {{");
                    sb.AppendLine($"{t}\t{typeDisplay} {p} = {PropProviderUtils.FormatConstant(symbol.Type, symbol.ExplicitDefaultValue)};");
                    if(next != null)
                        next(sb, tab+1);
                    sb.AppendLine($"{t}}}");
                }
                else if(symbol.Type.NullableAnnotation == NullableAnnotation.Annotated || symbol.Type.ToDisplayString().StartsWith("System.Nullable<"))
                {
                    sb.AppendLine($"{t}else {{");
                    sb.AppendLine($"{t}\t{typeDisplay} {p} = null;");
                    if(next != null)
                        next(sb, tab+1);
                    sb.AppendLine($"{t}}}");
                }
                else
                {
                    sb.AppendLine($"{t}else {{");
                    sb.AppendLine($"{t}\treturn new Result(400).Text(\"Body is required\");");
                    sb.AppendLine($"{t}}}");
                }
            });
}
