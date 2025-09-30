using Microsoft.CodeAnalysis;

namespace Celerio.Analyzers.Tests;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

public class RouteFormatAnalyzerTests
{
    private static async Task Test(string code)
    {
        var test = new CSharpAnalyzerTest<RouteFormatAnalyzer, XUnitVerifier>
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
    public async Task ReportsDiagnostic_MissingLeadingSlash()
    {
        var testCode = @"using Celerio;
using static Celerio.Result;

public static class Controller
{
    [|[Get(""api/users"")]|]
    public static Result Index() => Result.Ok();
}";

        await Test(testCode);
    }

    [Fact]
    public async Task ReportsDiagnostic_TrailingSlash()
    {
        var testCode = @"using Celerio;
using static Celerio.Result;

public static class Controller
{
    [|[Get(""/api/users/"")]|]
    public static Result Index() => Result.Ok();
}";

        await Test(testCode);
    }

    [Fact]
    public async Task ReportsDiagnostic_Backslash()
    {
        var testCode = @"using Celerio;
using static Celerio.Result;

public static class Controller
{
    [|[Get(@""\api\users"")]|]
    public static Result Index() => Result.Ok();
}";

        await Test(testCode);
    }

    [Fact]
    public async Task Supports_CorrectFormat()
    {
        var testCode = @"using Celerio;
using static Celerio.Result;

public static class Controller
{
    [Get(""/api/users"")]
    public static Result Index() => Result.Ok();
}";
        
        await Test(testCode);
    }
}
