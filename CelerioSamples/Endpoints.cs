using Celerio;

namespace CelerioSamples;

public static class Endpoints
{
    [EndpointRouter.Route("GET", "/test")]
    public static HttpResponse Test(HttpRequest request)
    {
        return HttpResponse.Ok("Hello, World!");
    }
}