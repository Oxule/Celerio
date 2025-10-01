namespace CelerioSamples;

using Celerio;
using static Celerio.Result;

public static class QueryParameters
{
    [Get("/search")]
    public static Result Search(string? query, int page = 0, int limit = 10)
    {
        if (string.IsNullOrEmpty(query))
            return Ok().Json(new {searchType = "global", results = new []{"placeholder", "placeholder"}, shown = 2, page, limit});
        return Ok().Json(new {searchType = "query", query, results = new []{$"placeholder with '{query}' value", $"placeholder with '{query}' value", $"placeholder with '{query}' value"}, shown = 3, page, limit});
    }

    [Get("/sum")]
    public static Result Sum(int a, int b)
    {
        return Ok().Text(a+b);
    }

    [Get("/force")]
    public static Result Force(float mass, float g = 9.8f)
    {
        return Ok().Text(mass*g);
    }
}
