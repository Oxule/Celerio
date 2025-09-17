using System;
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
    public class RoutePathVariableAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CEL13";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Malformed route path variable",
            messageFormat: "Route path contains malformed path variable(s): '{0}'. Suggestion: '{1}'",
            category: "Route",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

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

                    if (string.IsNullOrEmpty(route))
                        return;

                    if (RoutePathVariableHelper.TryFixRouteVariables(route, out var fixedRoute, out var hasProblem))
                    {
                        if (hasProblem)
                        {
                            var currentMethodSyntax = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as MethodDeclarationSyntax;
                            var reportLocation = currentMethodSyntax?.AttributeLists.FirstOrDefault()?.GetLocation()
                                                 ?? currentMethodSyntax?.Identifier.GetLocation()
                                                 ?? methodSymbol.Locations.FirstOrDefault();

                            var diagnostic = Diagnostic.Create(Rule, reportLocation ?? Location.None, route, fixedRoute);
                            symCtx.ReportDiagnostic(diagnostic);
                        }
                    }

                }, SymbolKind.Method);
            });
        }
    }

    internal static class RoutePathVariableHelper
{
    public static bool TryFixRouteVariables(string route, out string fixedRoute, out bool hasProblem)
    {
        if (route == null) throw new ArgumentNullException(nameof(route));

        var outSb = new System.Text.StringBuilder();
        int depth = 0; 
        hasProblem = false;

        for (int i = 0; i < route.Length; i++)
        {
            char c = route[i];

            if (c == '{')
            {
                if (depth == 0)
                {
                    
                    outSb.Append('{');
                    depth = 1;
                }
                else
                {
                    
                    hasProblem = true;
                    
                }
                continue;
            }

            if (c == '}')
            {
                if (depth == 0)
                {
                    
                    hasProblem = true;
                    int insertPos = FindPreviousSlashPosition(outSb);
                    int posToInsert = (insertPos >= 0) ? insertPos + 1 : 0;
                    outSb.Insert(posToInsert, '{');
                    outSb.Append('}');
                }
                else
                {
                    
                    outSb.Append('}');
                    depth = 0;

                    
                    if (i + 1 < route.Length && route[i + 1] == '{')
                    {
                        outSb.Append('/');
                        hasProblem = true;
                        
                    }
                }
                continue;
            }

            if (c == '/')
            {
                if (depth > 0)
                {
                    
                    outSb.Append('}');
                    depth = 0;
                    hasProblem = true;
                    outSb.Append('/');
                }
                else
                {
                    outSb.Append('/');
                }
                continue;
            }

            
            outSb.Append(c);
        }

        
        if (depth > 0)
        {
            outSb.Append('}');
            hasProblem = true;
            depth = 0;
        }

        fixedRoute = outSb.ToString();
        return true;
    }

    private static int FindPreviousSlashPosition(System.Text.StringBuilder sb)
    {
        for (int j = sb.Length - 1; j >= 0; j--)
        {
            if (sb[j] == '/') return j;
        }
        return -1;
    }
}


    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RoutePathVariableCodeFixProvider)), Shared]
    public class RoutePathVariableCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RoutePathVariableAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.FirstOrDefault();
            if (diagnostic == null) return;

            var cancellationToken = context.CancellationToken;
            var root = await context.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null) return;

            var node = root.FindNode(diagnostic.Location.SourceSpan);
            var methodDecl = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (methodDecl == null)
                methodDecl = node.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();

            if (methodDecl == null) return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Fix route path variables",
                    createChangedDocument: c => FixRouteLiteralAsync(context.Document, methodDecl, c),
                    equivalenceKey: "FixRoutePathVariables"),
                diagnostic);
        }

        private async Task<Document> FixRouteLiteralAsync(Document document, MethodDeclarationSyntax methodDecl, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null) return document;

            var methodSymbol = semanticModel.GetDeclaredSymbol(methodDecl, cancellationToken) as IMethodSymbol;
            if (methodSymbol == null) return document;

            var routeInfo = methodSymbol.GetRouteInfo(true);
            if (routeInfo == null) return document;

            var (httpMethod, route) = routeInfo.Value;
            if (route == null) return document;

            if (!RoutePathVariableHelper.TryFixRouteVariables(route, out var fixedRoute, out var hasProblem))
                return document;

            if (!hasProblem) return document;

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
                SyntaxFactory.Literal(fixedRoute))
                .WithLeadingTrivia(oldExpr.GetLeadingTrivia())
                .WithTrailingTrivia(oldExpr.GetTrailingTrivia());

            var newRoot = root.ReplaceNode(oldExpr, newLiteral);
            var newDoc = document.WithSyntaxRoot(newRoot);

            return newDoc;
        }
    }
}
