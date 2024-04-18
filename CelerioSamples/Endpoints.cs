using Celerio;

namespace CelerioSamples;

public static class Endpoints
{
    [Route("GET", "/")]
    public static HttpResponse Index(HttpRequest request)
    {
        return HttpResponse.Ok("Hello! This is Celerio Sample!");
    }
    
    [Route("GET", "/sum", "/add", "/add/{a}/{b}", "/sum/{a}/{b}")]
    public static HttpResponse Sum(int a, int b)
    {
        return HttpResponse.Ok((a+b).ToString());
    }
}