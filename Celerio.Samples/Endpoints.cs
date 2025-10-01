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

    [Get("/async")]
    public static async Task<Result> Async()
    {
        await Task.Delay(1000);
        return Ok().Text("*some work done*\nHello, Celerio 2.0!");
    }
}