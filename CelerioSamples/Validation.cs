using Celerio;

namespace CelerioSamples;

public static class Validation
{
    [Route("GET", "/validation/string")]
    public static object ValidationString([Length(6,2)] string text)
    {
        return text;
    }
}