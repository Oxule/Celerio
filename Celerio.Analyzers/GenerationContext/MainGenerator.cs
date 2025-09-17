using System.Diagnostics;
using System.Text;
using Celerio.Analyzers.Generators;
using Celerio.Analyzers.Generators.EndpointGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Celerio.Analyzers.GenerationContext;

[Generator]
public class MainGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var compilationProvider = context.CompilationProvider;
        
        var endpointsProvider = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (s, _) => s is MethodDeclarationSyntax m &&
                                            m.Modifiers.Any(SyntaxKind.PublicKeyword) &&
                                            m.Modifiers.Any(SyntaxKind.StaticKeyword),
                transform: static (ctx, _) =>
                {
                    var method = (MethodDeclarationSyntax)ctx.Node;
                    var symbol = ModelExtensions.GetDeclaredSymbol(ctx.SemanticModel, method) as IMethodSymbol;
                    if (symbol == null)
                        return null;
                    var route = symbol?.GetRouteInfo();
                    if (!route.HasValue)
                        return null;
                    return new Endpoint(route.Value.Method, route.Value.Path, symbol!);
                })
            .Where(symbol => symbol != null);
        
        var typesProvider = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (s, _) => s is TypeDeclarationSyntax m &&
                                            m.Modifiers.Any(SyntaxKind.PublicKeyword),
                transform: static (ctx, _) =>
                {
                    var symbol = ModelExtensions.GetDeclaredSymbol(ctx.SemanticModel, ctx.Node) as ITypeSymbol;
                    return symbol is not null && symbol.GetAttributes().Any(x=>x.AttributeClass?.ToString() == "Celerio.Serialize") ? symbol : null;
                })
            .Where(symbol => symbol != null);
        
        var combined = endpointsProvider.Collect().Combine(typesProvider.Collect())
            .Combine(compilationProvider);
        
        context.RegisterSourceOutput(combined, (spc, tuple) =>
        {
            var ((endpoints, types), compilation) = tuple;

            var generationContext = new GenerationContext();
            generationContext.Endpoints = endpoints.ToList();
            generationContext.Types = new (types.ToList());
            
            spc.AddSource("Router.g.cs", RouterGenerator.GenerateRouter(generationContext.Endpoints));
            spc.AddSource("Wrappers.g.cs", WrappersGenerator.GenerateWrappers(generationContext.Endpoints));
            spc.AddSource("Server.g.cs", ServerGenerator.GenerateServer());
            
            var sb = new StringBuilder();

            sb.AppendLine("namespace Celerio.Generated { public static class Test { public static void T() {} } }");
            

            spc.AddSource("Test.g.cs", sb.ToString());
        });
    }
}