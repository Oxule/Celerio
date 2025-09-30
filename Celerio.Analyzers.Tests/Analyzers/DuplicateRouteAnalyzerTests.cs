using Microsoft.CodeAnalysis;

namespace Celerio.Analyzers.Tests;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

public class DuplicateRouteAnalyzerTests
{
    private static async Task Test(string code)
    {
        var test = new CSharpAnalyzerTest<DuplicateRouteAnalyzer, XUnitVerifier>
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
    public async Task ReportsDiagnostic_DuplicateRoutes()
    {
        var testCode = @"using Celerio;
using static Celerio.Result;

public static class Controller
{
    [|[Get(""/"")]|]
    public static Result Index1() => Result.Ok();

    [|[Get(""/"")]|]
    public static Result Index2() => Result.Ok();
}";
        
        await Test(testCode);
    }
    
    [Fact]
    public async Task ReportsDiagnostic_DuplicateRoutesMixed()
    {
        var testCode = @"using Celerio;
using static Celerio.Result;

public static class Controller
{
    [|[Get(""/"")]|]
    public static Result Index1() => Result.Ok();

    [|[Route(""GET"",""/"")]|]
    public static Result Index2() => Result.Ok();
}";
        
        await Test(testCode);
    }

    [Fact]
    public async Task NoDiagnostic_DifferentMethods_SamePath()
    {
        var testCode = @"using Celerio;
using static Celerio.Result;

public static class Controller
{
    [Get(""/api"")]
    public static Result Index1() => Result.Ok();

    [Post(""/api"")]
    public static Result Index2() => Result.Ok();
}";
        
        await Test(testCode);
    }

    [Fact]
    public async Task NoDiagnostic_UniqueRoutes()
    {
        var testCode = @"using Celerio;
using static Celerio.Result;

public static class Controller
{
    [Get(""/"")]
    public static Result Index1() => Result.Ok();

    [Get(""/home"")]
    public static Result Index2() => Result.Ok();

    [Post(""/"")]
    public static Result Index3() => Result.Ok();
}";
        
        await Test(testCode);
    }
}
