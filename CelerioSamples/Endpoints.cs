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
    public static HttpResponse Auth(Dictionary<string, string> auth, Pipeline pipeline)
    {
        if (auth == null)
        {
            var t = ((DefaultAuthentification) pipeline.Authentification).GenerateToken(new Dictionary<string, string>()
                {{"name", "oxule"}});
            return new HttpResponse(200, "OK", new Dictionary<string, string>() {{"Set-Cookie", $"auth={t}"}}, "");
        }

        return HttpResponse.Ok(auth["name"]);
    }
}