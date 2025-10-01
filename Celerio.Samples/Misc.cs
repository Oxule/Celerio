namespace CelerioSamples;

using Celerio;
using static Celerio.Result;

public static class Misc
{
    [Get("/ip")]
    public static Result Ip(Request req)
    {
        return Ok().Text(req.Remote.ToString() ?? "No ip?");
    }
}