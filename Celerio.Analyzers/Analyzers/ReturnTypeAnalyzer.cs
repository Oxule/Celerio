using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Celerio.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReturnTypeAnalyzerAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor UnsupportedReturnTypeRule =
        new DiagnosticDescriptor(
            id: "CEL01",
            title: "Unsupported return-type",
            messageFormat: "Method '{0}' returns unsupported type '{1}'. 'Result' and 'Task<Result>' only allowed.",
            category: "Endpoint",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(UnsupportedReturnTypeRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    private static readonly string[] Allowed = ["Celerio.Result","Result","System.Threading.Tasks.Task<Result>", "System.Threading.Tasks.Task<Celerio.Result>","Task<Result>", "Task<Celerio.Result>"];
    
    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var methodSymbol = (IMethodSymbol)context.Symbol;
        
        if (!methodSymbol.DeclaredAccessibility.HasFlag(Accessibility.Public) || !methodSymbol.IsStatic)
            return;

        var route = methodSymbol.GetRouteInfo();
        if(route == null)
            return;
        
        if (!Allowed.Any(x=>x == methodSymbol.ReturnType.ToString()))
        {
            var syntaxRef = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxRef != null)
            {
                var methodSyntax = syntaxRef.GetSyntax() as MethodDeclarationSyntax;
                if (methodSyntax != null)
                {
                    var returnTypeLocation = methodSyntax.ReturnType.GetLocation();

                    var diagnostic = Diagnostic.Create(
                        UnsupportedReturnTypeRule,
                        returnTypeLocation,
                        methodSymbol.Name,
                        methodSymbol.ReturnType.ToString());

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
    
}