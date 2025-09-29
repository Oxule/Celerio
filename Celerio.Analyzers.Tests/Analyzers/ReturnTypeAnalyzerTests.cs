using Microsoft.CodeAnalysis;

namespace Celerio.Analyzers.Tests;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

public class ReturnTypeAnalyzerTests
{
    private static async Task Test(string code)
    {
        var test = new CSharpAnalyzerTest<ReturnTypeAnalyzer, XUnitVerifier>
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
    public async Task ReportsDiagnostic_Int()
    {
        var testCode = @"using Celerio;
using static Celerio.Result;

public static class Controller
{
    [Get(""/"")]
    public static [|int|] Index()
    {
        return 5;
    }
}";
        
        await Test(testCode);
    }
    [Fact]
    public async Task ReportsDiagnostic_Void()
    {
        var testCode = @"using Celerio;
using static Celerio.Result;

public static class Controller
{
    [Get(""/"")]
    public static [|void|] Index() { }
}";
        
        await Test(testCode);
    }
    [Fact]
    public async Task Supported_Result()
    {
        var testCode = @"using Celerio;
using static Celerio.Result;

public static class Controller
{
    [Get(""/"")]
    public static Result Index() 
    {
        return Result.Ok();
    }
}";
        
        await Test(testCode);
    }
    
    [Fact]
    public async Task Supported_AsyncResult()
    {
        var testCode = @"using System.Threading.Tasks;
using Celerio;
using static Celerio.Result;

public static class Controller
{
    [Get(""/"")]
    public static async Task<Result> Index() 
    {
        return Result.Ok();
    }
}";
        
        await Test(testCode);
    }
    
    [Fact]
    public async Task ReportsDiagnostic_AsyncResult()
    {
        var testCode = @"using System.Threading.Tasks;
using Celerio;
using static Celerio.Result;

public static class Controller
{
    [Get(""/"")]
    public static async [|Task|] Index() { }
}";
        
        await Test(testCode);
    }
}