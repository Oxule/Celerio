using Microsoft.CodeAnalysis;

namespace Celerio.Analyzers.Tests;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

public class RouteValidationAnalyzerTests
{
    private static async Task Test(string code)
    {
        var test = new CSharpAnalyzerTest<RouteValidationAnalyzer, XUnitVerifier>
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
    public async Task ReportsDiagnostic_RouteAttribute_EmptyMethod()
    {
        var testCode = @"using Celerio;
using static Celerio.Result;

public static class Controller
{
    [|[Route("""", ""/api"")]|]
    public static Result Index() => Result.Ok();
}";

        await Test(testCode);
    }

    [Fact]
    public async Task ReportsDiagnostic_RouteAttribute_EmptyPattern()
    {
        var testCode = @"using Celerio;
using static Celerio.Result;

public static class Controller
{
    [|[Route(""GET"", """")]|]
    public static Result Index() => Result.Ok();
}";

        await Test(testCode);
    }

    [Fact]
    public async Task ReportsDiagnostic_GetAttribute_EmptyPattern()
    {
        var testCode = @"using Celerio;
using static Celerio.Result;

public static class Controller
{
    [|[Get("""")]|]
    public static Result Index() => Result.Ok();
}";

        await Test(testCode);
    }

    [Fact]
    public async Task Supports_ValidRoute()
    {
        var testCode = @"using Celerio;
using static Celerio.Result;

public static class Controller
{
    [Get(""/api"")]
    public static Result Index() => Result.Ok();
}";
        
        await Test(testCode);
    }
}
