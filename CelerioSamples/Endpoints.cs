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
    public static HttpResponse Test(string a, int b, bool c = true, float d = 1.1f)
    {
        return HttpResponse.Ok($"{a} {b} {c} {d}");
    }
    
    [Route("POST", "/body")]
    public static HttpResponse body(HttpRequest req)
    {
        return HttpResponse.Ok(req.Body);
    }
    
    [Route("GET", "/sum", "/add", "/add/{a}/{b}", "/sum/{a}/{b}")]
    public static HttpResponse Sum(int a, int b)
    {
        return HttpResponse.Ok((a+b).ToString());
    }
    
    [Route("GET", "/auth")]
    public static HttpResponse Auth(HttpRequest request)
    {
        string hs = "";
        foreach (var h in request.Headers)
        {
            hs += $"{h.Key}: {h.Value}\n";
        }
        var r = HttpResponse.Ok(hs);
        r.Headers["Authorization"] = $"Bearer {DateTime.Now.Millisecond}";
        return r;
    }
}