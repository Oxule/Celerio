using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Celerio.Analyzers;

public static class SymbolExtensions
{
    public static string GetFullSymbolPath(this ISymbol symbol)
    {
        var sb = new StringBuilder();

        if (!symbol.ContainingNamespace.IsGlobalNamespace)
        {
            sb.Append(symbol.ContainingNamespace.ToDisplayString());
            sb.Append('.');
        }

        var containingType = symbol.ContainingType;
        if (containingType != null)
        {
            sb.Append(containingType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            sb.Append('.');
        }

        sb.Append(symbol.Name);
        
        return sb.ToString();
    }
    
    public static HashSet<ITypeSymbol> GetAllReturnTypes(
        this IMethodSymbol method,
        Compilation compilation,
        HashSet<IMethodSymbol>? visitedMethods = null)
    {
        visitedMethods ??= new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
        var result = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        if (visitedMethods.Contains(method))
            return result;

        visitedMethods.Add(method);

        if (method.DeclaringSyntaxReferences.Length == 0)
        {
            if (!IsExcludedType(method.ReturnType))
                result.Add(method.ReturnType);
            return result;
        }

        foreach (var syntaxRef in method.DeclaringSyntaxReferences)
        {
            var syntaxTree = syntaxRef.SyntaxTree;

            if (!compilation.SyntaxTrees.Contains(syntaxTree))
            {
                if (!IsExcludedType(method.ReturnType))
                    result.Add(method.ReturnType);
                continue;
            }

            var syntax = syntaxRef.GetSyntax();
            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            IEnumerable<ExpressionSyntax> returnExpressions = Enumerable.Empty<ExpressionSyntax>();

            switch (syntax)
            {
                case MethodDeclarationSyntax mds when mds.Body != null:
                    returnExpressions = mds.Body.DescendantNodes()
                        .OfType<ReturnStatementSyntax>()
                        .Select(rs => rs.Expression)
                        .Where(e => e != null)!;
                    break;

                case MethodDeclarationSyntax mds when mds.ExpressionBody != null:
                    returnExpressions = new[] { mds.ExpressionBody.Expression };
                    break;
            }

            foreach (var expr in returnExpressions)
            {
                if (expr == null) continue;

                var typeInfo = semanticModel.GetTypeInfo(expr);
                if (typeInfo.Type == null)
                    continue;

                var type = typeInfo.Type;

                if (expr is InvocationExpressionSyntax invoc)
                {
                    var calledSymbol = semanticModel.GetSymbolInfo(invoc).Symbol as IMethodSymbol;
                    if (calledSymbol != null)
                    {
                        var nestedTypes = calledSymbol.GetAllReturnTypes(compilation, visitedMethods);
                        foreach (var t in nestedTypes)
                            if (!IsExcludedType(t))
                                result.Add(t);
                        continue;
                    }
                }

                if (!IsExcludedType(type))
                    result.Add(type);
            }
        }

        return result;
    }
    private static bool IsExcludedType(ITypeSymbol type)
    {
        if (type == null) return true;
        if (type.SpecialType == SpecialType.System_Object) return true;
        if (type.TypeKind == TypeKind.Dynamic) return true;
        return false;
    }
}