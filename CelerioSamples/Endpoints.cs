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
    
    [Route("GET", "/calc/{method}/{a}/{b}", "/calc/{method}")]
    public static HttpResponse Calc(int a, int b, string method)
    {
        switch (method)
        {
            case "add":
                return HttpResponse.Ok((a+b).ToString());
            case "sub":
                return HttpResponse.Ok((a-b).ToString());
            case "mul":
                return HttpResponse.Ok((a*b).ToString());
            case "div":
                return HttpResponse.Ok((a/b).ToString());
        }
        return HttpResponse.Ok((a+b).ToString());
    }
    
    [Route("GET", "/auth/{user}")]
    public static HttpResponse Auth(string user, Pipeline pipeline)
    {
        return pipeline.Authentification.SendAuthentification(new Dictionary<string, string>() {{"user", user}});
    }
    
    [Authentificated]
    [Route("GET", "/auth")]
    public static HttpResponse AuthCheck(Dictionary<string, string> auth)
    {
        return HttpResponse.Ok(auth["user"]);
    }
    
    [Cached(5)]
    [Route("GET", "/cache")]
    public static HttpResponse Cached(HttpRequest request)
    {
        return HttpResponse.Ok(DateTime.UtcNow.ToString("G"));
    }
    
    [Cached(60*60*24*30)]
    [Route("GET", "/image/{name}")]
    public static HttpResponse Image(string name)
    {
        return HttpResponse.File(name, "image/jpeg");
    }
}