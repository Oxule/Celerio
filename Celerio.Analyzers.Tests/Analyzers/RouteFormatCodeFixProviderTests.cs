using Microsoft.CodeAnalysis;

namespace Celerio.Analyzers.Tests;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

public class RouteFormatCodeFixProviderTests
{
    private static async Task Test(string code, string fixedCode)
    {
        var test = new CSharpCodeFixTest<RouteFormatAnalyzer, RouteFormatCodeFixProvider, XUnitVerifier>
        {
            TestCode = code,
            FixedCode = fixedCode,
            TestState =
            {
                AdditionalReferences = {
                    MetadataReference.CreateFromFile(typeof(Celerio.Result).Assembly.Location) }
            }
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task FixMissingLeadingSlash()
    {
        var code = @"using Celerio;
using static Celerio.Result;

public static class Controller
{
    [|[Get(@""api/users"")]|]
    public static Result Index() => Result.Ok();
}";

        var fixedCode = @"using Celerio;
using static Celerio.Result;

public static class Controller
{
    [Get(""/api/users"")]
    public static Result Index() => Result.Ok();
}";

        await Test(code, fixedCode);
    }

    [Fact]
    public async Task FixTrailingSlash()
    {
        var code = @"using Celerio;
using static Celerio.Result;

public static class Controller
{
    [|[Get(@""/api/users/"")]|]
    public static Result Index() => Result.Ok();
}";

        var fixedCode = @"using Celerio;
using static Celerio.Result;

public static class Controller
{
    [Get(""/api/users"")]
    public static Result Index() => Result.Ok();
}";

        await Test(code, fixedCode);
    }

    [Fact]
    public async Task FixBackslash()
    {
        var code = @"using Celerio;
using static Celerio.Result;

public static class Controller
{
    [|[Get(@""\api\users"")]|]
    public static Result Index() => Result.Ok();
}";

        var fixedCode = @"using Celerio;
using static Celerio.Result;

public static class Controller
{
    [Get(""/api/users"")]
    public static Result Index() => Result.Ok();
}";

        await Test(code, fixedCode);
    }
}
