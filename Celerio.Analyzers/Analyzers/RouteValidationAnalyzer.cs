using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Celerio.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RouteValidationAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CEL14";

        private static readonly DiagnosticDescriptor RouteValidationRule =
            new DiagnosticDescriptor(
                id: DiagnosticId,
                title: "Null or empty value in route attribute",
                messageFormat: "Route attribute '{0}' for method '{1}' has null or empty value for parameter '{2}'.",
                category: "Route",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(RouteValidationRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compStartCtx =>
            {
                compStartCtx.RegisterSymbolAction(symCtx =>
                {
                    var methodSymbol = (IMethodSymbol)symCtx.Symbol;

                    if (!methodSymbol.DeclaredAccessibility.HasFlag(Accessibility.Public) || !methodSymbol.IsStatic)
                        return;

                    foreach (var attr in methodSymbol.GetAttributes())
                    {
                        var fullName = attr?.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        if (fullName == null) continue;

                        if (fullName == "global::Celerio.RouteAttribute")
                        {
                            if (attr.ConstructorArguments.Length == 2)
                            {
                                var methodArg = attr.ConstructorArguments[0];
                                var patternArg = attr.ConstructorArguments[1];

                                if (!IsValidStringValue(methodArg.Value))
                                {
                                    ReportDiagnostic(symCtx, attr, "Route", methodSymbol, "Method");
                                }

                                if (!IsValidStringValue(patternArg.Value))
                                {
                                    ReportDiagnostic(symCtx, attr, "Route", methodSymbol, "Pattern");
                                }
                            }
                        }
                        else if (fullName is "global::Celerio.GetAttribute"
                                 or "global::Celerio.PostAttribute"
                                 or "global::Celerio.PutAttribute"
                                 or "global::Celerio.DeleteAttribute"
                                 or "global::Celerio.PatchAttribute")
                        {
                            if (attr.ConstructorArguments.Length == 1)
                            {
                                var patternArg = attr.ConstructorArguments[0];

                                if (!IsValidStringValue(patternArg.Value))
                                {
                                    var attrName = fullName.Replace("global::Celerio.", "").Replace("Attribute", "");
                                    ReportDiagnostic(symCtx, attr, attrName, methodSymbol, "Pattern");
                                }
                            }
                        }
                    }
                }, SymbolKind.Method);
            });
        }

        private static bool IsValidStringValue(object value)
        {
            if (value == null) return false;
            if (value is string s) return s.Length > 0;
            return false;
        }

        private static void ReportDiagnostic(SymbolAnalysisContext context, AttributeData attr, string attrName, IMethodSymbol methodSymbol, string paramName)
        {
            var currentMethodSyntax = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as MethodDeclarationSyntax;
            var reportLocation = currentMethodSyntax?.AttributeLists.FirstOrDefault()?.GetLocation()
                                 ?? currentMethodSyntax?.Identifier.GetLocation()
                                 ?? methodSymbol.Locations.FirstOrDefault();

            var diagnostic = Diagnostic.Create(RouteValidationRule, reportLocation ?? Location.None, attrName, methodSymbol.Name, paramName);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
