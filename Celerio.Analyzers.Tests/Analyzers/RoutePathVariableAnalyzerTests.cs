using Microsoft.CodeAnalysis;

namespace Celerio.Analyzers.Tests;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

public class RoutePathVariableAnalyzerTests
{
    private static async Task Test(string code)
    {
        var test = new CSharpAnalyzerTest<RoutePathVariableAnalyzer, XUnitVerifier>
        {
            TestCode = code,
            TestState =
            {
                AdditionalReferences = {
                    MetadataReference.CreateFromFile(typeof(Celerio.Result).Assembly.Location) }
            }
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsDiagnostic_AdjacentVariables()
    {
        var testCode = @"using Celerio;
using static Celerio.Result;

public static class Controller
{
    [|[Get(""/users/{id}{name}"")]|]
    public static Result Index() => Result.Ok();
}";

        await Test(testCode);
    }

    [Fact]
    public async Task Supports_CorrectVariables()
    {
        var testCode = @"using Celerio;
using static Celerio.Result;

public static class Controller
{
    [Get(""/users/{id}/posts/{postId}"")]
    public static Result Index() => Result.Ok();
}";
        
        await Test(testCode);
    }
}
