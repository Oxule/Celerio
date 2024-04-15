using Celerio;

namespace CelerioSamples;

public static class Endpoints
{
    [Route("GET", "/")]
    public static HttpResponse Index(HttpRequest request)
    {
        return HttpResponse.Ok($"{request.Method}");
    }
    
    [Route("GET", "/test")]
    public static HttpResponse Test(HttpRequest request, string a, int b, bool c = true, float d = 1.1f)
    {
        return HttpResponse.Ok($"{a} {b} {c} {d}");
    }
    
    [Route("GET", "/sum", "/add", "/add/{a}/{b}", "/sum/{a}/{b}")]
    public static HttpResponse Sum(int a, int b)
    {
        return HttpResponse.Ok((a+b).ToString());
    }
}