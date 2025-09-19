using Microsoft.CodeAnalysis;

namespace Celerio.Analyzers;

public static class IMethodSymbolExtention
{
    public static (string Method, string Path)? GetRouteInfo(this IMethodSymbol methodSymbol, bool raw = false)
    {
        string FixRoute(string route)
        {
            if (raw)
                return route;
            return PreprocessRoute(route);
        }
        
        foreach (var attr in methodSymbol.GetAttributes())
        {
            var fullName = attr?.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (fullName == null) continue;

            if (fullName == "global::Celerio.RouteAttribute")
            {
                if (attr.ConstructorArguments.Length == 2)
                {
                    var httpMethod = attr.ConstructorArguments[0].Value?.ToString().ToUpperInvariant();
                    var path = attr.ConstructorArguments[1].Value?.ToString() ?? "";
                    return httpMethod != null ? (httpMethod, FixRoute(path)) : null;
                }
            }
            else if (fullName is "global::Celerio.GetAttribute"
                     or "global::Celerio.PostAttribute"
                     or "global::Celerio.PutAttribute"
                     or "global::Celerio.DeleteAttribute"
                     or "global::Celerio.PatchAttribute")
            {
                var httpMethod = fullName.Replace("global::Celerio.", "").Replace("Attribute", "").ToUpperInvariant();
                if (attr.ConstructorArguments.Length == 1)
                {
                    var path = attr.ConstructorArguments[0].Value?.ToString() ?? "";
                    return (httpMethod, FixRoute(path));
                }
            }
        }

        return null;
    }

    internal static string PreprocessRoute(string route)
    {
        var trimmedEnd = route.Trim().Replace('\\','/').TrimEnd('/');
        if (trimmedEnd.Length == 0)
            return "/";
        if (trimmedEnd[0] != '/')
            return "/"+trimmedEnd;
        return trimmedEnd;
    }
}