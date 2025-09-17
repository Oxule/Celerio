using System.Text;
using System.Text.RegularExpressions;
using Celerio.Analyzers.GenerationContext;
using Microsoft.CodeAnalysis;

namespace Celerio.Analyzers.Generators.EndpointGenerator;

public static class WrappersGenerator
{
    private static readonly Regex ParamRegex = new Regex(@"\{([^{}\/]+)\}", RegexOptions.Compiled);

    private static string GenerateDeserializer(IParameterSymbol symbol, string getter)
    {
        switch (symbol.Type.ToString())
        {
            case "string":
                return getter;
            case "int":
                return $"int.Parse({getter})";
            default:
                return $"JsonSerializer.Deserialize<{symbol.Type}>({getter})";
        }
    }
    
    private static string GenerateProps(StringBuilder sb, List<string> pathVariables, IMethodSymbol symbol)
    {
        if (symbol.Parameters.Length == 0)
            return "";
        
        List<string> props = new List<string>();

        foreach (var p in symbol.Parameters)
        {
            if (p.HasExplicitDefaultValue)
            {
                sb.AppendLine($"\t\t\t{p.Type} parameter_{p.Name} = {p.ExplicitDefaultValue};");
            }
            else
            {
                sb.AppendLine($"\t\t\t{p.Type} parameter_{p.Name};");
            }
        }
        
        sb.AppendLine("\t\t\ttry {");
        
        foreach (var p in symbol.Parameters)
        {
            var getter = GenerateDeserializer(p,$"request.Query[\"{p.Name}\"]");
            if (pathVariables.Contains(p.Name))
                getter = GenerateDeserializer(p,$"path_{p.Name}");
            else if (p.Name == "body")
                getter = GenerateDeserializer(p,"request.Body");
            else if (p.Type.ToString() == "Celerio.Request")
                getter = $"request";

            sb.AppendLine($"\t\t\t\tparameter_{p.Name} = {getter};");
            props.Add($"parameter_{p.Name}");
        }
        
        sb.AppendLine("\t\t\t} catch (Exception e) {");
        sb.AppendLine("\t\t\t\treturn new Result(400).Text(e.ToString());");
        sb.AppendLine("\t\t\t}");
        
        return string.Join(", ", props);
    }
    
    private static void GenerateWrapper(IMethodSymbol symbol, StringBuilder sb)
    {
        string name = symbol.GetFullSymbolPath().Replace('.','_') + "_Wrapper";

        var route = symbol.GetRouteInfo()!;
        var matches = ParamRegex.Matches(route.Value.Path);
        var pathVariables = new List<string>(matches.Count);
        foreach (Match m in matches)
            pathVariables.Add(m.Groups[1].Value);
        
        string header = $"\t\tpublic static async Task<Result> {name}({string.Join(", ", ["Celerio.Request request",..pathVariables.Select(x => "string path_" + x)])})";
        sb.AppendLine(header + " {");

        var props = GenerateProps(sb, pathVariables, symbol);
        
        
        sb.AppendLine("\t\t\ttry {");

        if (symbol.ReturnType.ToString() == "System.Threading.Tasks.Task<Celerio.Result>")
        {
            sb.AppendLine($"\t\t\t\treturn await {symbol.GetFullSymbolPath()}({props});");
        }
        else
        {
            sb.AppendLine($"\t\t\t\treturn {symbol.GetFullSymbolPath()}({props});");
        }
        
        sb.AppendLine("\t\t\t} catch (Exception e) {");
        sb.AppendLine("\t\t\t\treturn new Result(500).Text(e.ToString());");
        sb.AppendLine("\t\t\t}");
        sb.AppendLine("\t\t}");
    }
    
    public static string GenerateWrappers(List<Endpoint> endpoints)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System.Text.Json;");
        
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