using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using System.Composition;

namespace Celerio.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RouteFormatAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CEL12";

        private static readonly DiagnosticDescriptor RouteFormatRule =
            new DiagnosticDescriptor(
                id: DiagnosticId,
                title: "Undesirable route format",
                messageFormat: "Route for method '{0}' with path '{1}' is written in wrong format. Better to write: '{2}'.",
                category: "Route",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(RouteFormatRule);

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

                    var routeInfo = methodSymbol.GetRouteInfo(true);
                    if (routeInfo == null)
                        return;

                    var (httpMethod, route) = routeInfo.Value;
                    var correct = IMethodSymbolExtention.PreprocessRoute(route);
                    if (correct != route)
                    {
                        var currentMethodSyntax = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as MethodDeclarationSyntax;
                        var reportLocation = currentMethodSyntax?.AttributeLists.FirstOrDefault()?.GetLocation()
                                             ?? currentMethodSyntax?.Identifier.GetLocation()
                                             ?? methodSymbol.Locations.FirstOrDefault();

                        var diagnostic = Diagnostic.Create(RouteFormatRule, reportLocation ?? Location.None, methodSymbol.Name, route, correct);
                        symCtx.ReportDiagnostic(diagnostic);
                    }

                }, SymbolKind.Method);
            });
        }
    }

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RouteFormatCodeFixProvider)), Shared]
    public class RouteFormatCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RouteFormatAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var cancellationToken = context.CancellationToken;
            var root = await context.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null) return;

            var node = root.FindNode(diagnostic.Location.SourceSpan);
            var methodDecl = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (methodDecl == null)
            {
                var possibleMethod = node.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                methodDecl = possibleMethod;
            }

            if (methodDecl == null)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Fix route format",
                    createChangedDocument: c => ReplaceRouteAsync(context.Document, methodDecl, c),
                    equivalenceKey: "FixRouteFormat"),
                diagnostic);
        }

        private async Task<Document> ReplaceRouteAsync(Document document, MethodDeclarationSyntax methodDecl, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null) return document;

            var methodSymbol = semanticModel.GetDeclaredSymbol(methodDecl, cancellationToken) as IMethodSymbol;
            if (methodSymbol == null) return document;

            var routeInfo = methodSymbol.GetRouteInfo(true);
            if (routeInfo == null) return document;

            var (httpMethod, route) = routeInfo.Value;
            var correct = IMethodSymbolExtention.PreprocessRoute(route);
            if (correct == route) return document;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null) return document;

            AttributeArgumentSyntax targetArg = null;

            foreach (var attr in methodDecl.AttributeLists.SelectMany(al => al.Attributes))
            {
                var args = attr.ArgumentList?.Arguments;
                if (args == null) continue;

                foreach (var arg in args)
                {
                    var expr = arg.Expression;
                    if (expr == null) continue;

                    var constValue = semanticModel.GetConstantValue(expr, cancellationToken);
                    if (!constValue.HasValue) continue;
                    if (constValue.Value is string s && s == route)
                    {
                        targetArg = arg;
                        break;
                    }
                }

                if (targetArg != null) break;
            }

            if (targetArg == null)
            {
                foreach (var attr in methodDecl.AttributeLists.SelectMany(al => al.Attributes))
                {
                    var args = attr.ArgumentList?.Arguments;
                    if (args == null) continue;

                    foreach (var arg in args)
                    {
                        if (arg.Expression is LiteralExpressionSyntax lit && lit.IsKind(SyntaxKind.StringLiteralExpression))
                        {
                            targetArg = arg;
                            break;
                        }
                    }

                    if (targetArg != null) break;
                }
            }

            if (targetArg == null) return document;

            var oldExpr = targetArg.Expression;
            var newLiteral = SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(correct))
                .WithLeadingTrivia(oldExpr.GetLeadingTrivia())
                .WithTrailingTrivia(oldExpr.GetTrailingTrivia());

            var newRoot = root.ReplaceNode(oldExpr, newLiteral);
            var newDoc = document.WithSyntaxRoot(newRoot);

            return newDoc;
        }
    }
}
