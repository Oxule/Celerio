using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Celerio.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DuplicateRouteAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor DuplicateRouteRule =
            new DiagnosticDescriptor(
                id: "CEL11",
                title: "Duplicate route",
                messageFormat: "Route for HTTP method '{0}' and path '{1}' is duplicate. Also defined in: {2}",
                category: "Route",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(DuplicateRouteRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compStartCtx =>
            {
                var seen = new ConcurrentDictionary<string, ConcurrentBag<IMethodSymbol>>(StringComparer.Ordinal);

                compStartCtx.RegisterSymbolAction(symCtx =>
                {
                    var methodSymbol = (IMethodSymbol)symCtx.Symbol;

                    if (!methodSymbol.DeclaredAccessibility.HasFlag(Accessibility.Public) || !methodSymbol.IsStatic)
                        return;

                    var routeInfo = methodSymbol.GetRouteInfo();
                    if (routeInfo == null)
                        return;

                    var (httpMethod, route) = routeInfo.Value;
                    var key = httpMethod + ":" + route;

                    var bag = seen.GetOrAdd(key, _ => new ConcurrentBag<IMethodSymbol>());
                    bag.Add(methodSymbol);

                    var entries = bag.ToArray();
                    if (entries.Length <= 1)
                        return;

                    var also = string.Join(", ", entries.Select(s => s.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));

                    var currentMethodSyntax = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as MethodDeclarationSyntax;
                    var reportLocation = currentMethodSyntax?.AttributeLists.FirstOrDefault()?.GetLocation()
                                         ?? currentMethodSyntax?.Identifier.GetLocation()
                                         ?? methodSymbol.Locations.FirstOrDefault();

                    var diagnostic = Diagnostic.Create(DuplicateRouteRule, reportLocation ?? Location.None, httpMethod, route, also);
                    symCtx.ReportDiagnostic(diagnostic);

                    foreach (var prev in entries)
                    {
                        if (prev.Equals(methodSymbol))
                            continue;

                        var prevMethodSyntax = prev.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as MethodDeclarationSyntax;
                        var prevLoc = prevMethodSyntax?.AttributeLists.FirstOrDefault()?.GetLocation()
                                      ?? prevMethodSyntax?.Identifier.GetLocation()
                                      ?? prev.Locations.FirstOrDefault();

                        var diagPrev = Diagnostic.Create(DuplicateRouteRule, prevLoc ?? Location.None, httpMethod, route, also);
                        symCtx.ReportDiagnostic(diagPrev);
                    }

                }, SymbolKind.Method);
            });
        }
    }
}
