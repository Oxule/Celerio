namespace CelerioSamples;

using Celerio;
using static Celerio.Result;

public static class Parameters
{
    [Get("/params/query/string")]
    public static Result Query_String(string a)
    {
        return Ok().Text(a);
    }
    [Get("/params/query/int")]
    public static Result Query_Int(int a)
    {
        return Ok().Text(a);
    }
}