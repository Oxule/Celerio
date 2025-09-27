using Microsoft.CodeAnalysis;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Celerio.Analyzers.Generators;

public static class AuthPropProvider
{
    public static PropProvider Provider =
        new ((x)=>x.Name.ToLower() == "auth", 1, 10,
            (symbol, sb, tab, next) =>
            {
                var t = Tabs.Tab(tab);
                var p = $"parameter_{symbol.Name}";
                var typeDisplay = symbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                sb.AppendLine($"{t}var authHeader_{symbol.Name} = request.Headers.Get(\"Authorization\");");

                sb.AppendLine($"{t}if (authHeader_{symbol.Name}?.StartsWith(\"Bearer \") == true) {{");
                sb.AppendLine($"{t}\tvar token = authHeader_{symbol.Name}[7..]; // Remove 'Bearer ' prefix");

                sb.AppendLine($"{t}\tvar payloadJson = JWT.ValidateToken(token);");

                sb.AppendLine($"{t}\tif (payloadJson != null) {{");
                sb.AppendLine($"{t}\t\t{typeDisplay} {p} = JsonSerializer.Deserialize<{typeDisplay}>(payloadJson);");
                if(next != null)
                    next(sb, tab + 2);
                sb.AppendLine($"{t}\t}}");
                sb.AppendLine($"{t}\telse {{");
                sb.AppendLine($"{t}\t\treturn new Result(401).Text(\"Invalid token\");");
                sb.AppendLine($"{t}\t}}");
                sb.AppendLine($"{t}");

                if (symbol.Type.NullableAnnotation == NullableAnnotation.Annotated ||
                    symbol.Type.ToDisplayString().StartsWith("System.Nullable<")) {
                    sb.AppendLine($"{t}}}");
                    sb.AppendLine($"{t}else {{");
                    sb.AppendLine($"{t}\t{typeDisplay} {p} = null;");
                    if(next != null)
                        next(sb, tab + 1);
                    sb.AppendLine($"{t}}}");
                }
                else {
                    sb.AppendLine($"{t}}}");
                    sb.AppendLine($"{t}else {{");
                    sb.AppendLine($"{t}\treturn new Result(401).Text(\"Authorization header required\");");
                    sb.AppendLine($"{t}}}");
                }
            });
}
