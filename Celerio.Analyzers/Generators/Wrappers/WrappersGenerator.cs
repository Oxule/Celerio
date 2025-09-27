using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Celerio.Analyzers.GenerationContext;
using Microsoft.CodeAnalysis;

namespace Celerio.Analyzers.Generators;

public static class WrappersGenerator
{
    private static readonly Regex ParamRegex = new Regex(@"\{([^{}\/]+)\}", RegexOptions.Compiled);

    public static void GenerateCall(IMethodSymbol symbol, StringBuilder sb, string props, int tabs = 4)
    {
        var t = Tabs.Tab(tabs);
        sb.AppendLine($"{t}try {{");

        if (symbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Threading.Tasks.Task<global::Celerio.Result>")
        {
            sb.AppendLine($"{t}\treturn await {symbol.GetFullSymbolPath()}({props});");
        }
        else
        {
            sb.AppendLine($"{t}\treturn {symbol.GetFullSymbolPath()}({props});");
        }

        sb.AppendLine($"{t}}} catch (Exception e) {{");
        sb.AppendLine($"{t}\treturn new Result(500).Text(e.ToString());");
        sb.AppendLine($"{t}}}");
    }
    
    private static void GenerateWrapper(IMethodSymbol symbol, StringBuilder sb)
    {
        string name = symbol.GetFullSymbolPath().Replace('.', '_') + "_Wrapper";

        var route = symbol.GetRouteInfo()!;
        var matches = ParamRegex.Matches(route.Value.Path);
        var pathVariables = new List<string>(matches.Count);
        foreach (Match m in matches)
            pathVariables.Add(m.Groups[1].Value);

        string header =
            $"\t\tpublic static async Task<Result> {name}({string.Join(", ", ["global::Celerio.Request request", ..pathVariables.Select(x => "string path_" + x)])})";
        sb.AppendLine(header + " {");
        
        PropsGenerator.GenerateProps(sb, pathVariables, symbol);
        
        sb.AppendLine("\t\t}");
    }

    public static string GenerateWrappers(List<Endpoint> endpoints)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine("using System.Text.RegularExpressions;");
        sb.AppendLine("using Celerio;");

        sb.AppendLine("#pragma warning disable CS1998");
        sb.AppendLine("namespace Celerio.Generated {\n\tpublic static class EndpointWrappers {");
        foreach (var e in endpoints)
        {
            GenerateWrapper(e.Symbol, sb);
        }

        sb.AppendLine("\t}\n}");
        return sb.ToString();
    }
}
