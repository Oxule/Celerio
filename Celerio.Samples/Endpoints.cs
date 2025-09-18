namespace CelerioSamples;

using Celerio;
using static Celerio.Result;

public static class Endpoints
{
    [Get("/")]
    public static Result Index()
    {
        return Ok().Text("Hello, Celerio 2.0");
    }
    
    [Get("/random")]
    public static Result Random()
    {
        return Ok().Text($"Hello, User #{new Random().Next(1000000)}!");
    }

    [Get("/work")]
    public static async Task<Result> Workload()
    {
        await Task.Delay(1000);
        return Ok().Text("Some work done");
    }
    
    [Get("/path/{variable}/test")]
    public static Result PathTest()
    {
        return Ok().Text("Variable path test");
    }
    [Get("/path/{variable}")]
    public static Result Path()
    {
        return Ok().Text("Variable path not test (visit /path/variable/test)");
    }
    [Get("/path/test")]
    public static Result PathOverride()
    {
        return Ok().Text("Variable path override");
    }
    
    [Get("/html")]
    public static Result Html()
    {
        return Ok().Html("<span>Some HTML page.<b><br/>DO NOT USE FOR FRONTEND - USE REACT!!!</b></span>");
    }

    [Get("/sum")]
    public static Result Sum(int a, int b)
    {
        return Ok().Text((a + b).ToString());
    }
}