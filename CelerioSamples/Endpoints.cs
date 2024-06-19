using System.Globalization;
using Celerio;
using Newtonsoft.Json;

namespace CelerioSamples;

[Service("SampleService", "Sample Description")]
public static class Endpoints
{
    [Route("GET", "/")]
    public static HttpResponse Index(HttpRequest request)
    {
        return HttpResponse.Ok("Hello! This is Celerio Sample!");
    }


    public enum SampleEnum
    {
        A,
        B
    }
    [Route("GET", "/enum")]
    public static HttpResponse EnumTest(SampleEnum e)
    {
        switch (e)
        {
            case SampleEnum.A:
                return HttpResponse.Ok("AHAHAHA it works");
            case SampleEnum.B:
                return HttpResponse.Ok("AHAHAHA it works but B");
        }
        return HttpResponse.Ok("Awwww..");
    }
    
    [Route("GET", "/sum", "/add", "/add/{a}/{b}", "/sum/{a}/{b}")]
    public static HttpResponse Sum(int a, int b)
    {
        return HttpResponse.Ok((a+b).ToString());
    }
    
    [Route("GET", "/default")]
    public static HttpResponse DefaultValues(int a = 5, float b = 5.5f, string c = "ggg", bool d = false)
    {
        return HttpResponse.Ok("");
    }

    public record User
    {
        public string Name;
        public int Age;
    }
    
    [Route("POST", "/user")]
    public static HttpResponse user(User body)
    {
        return HttpResponse.Ok(body.Name);
    }
    
    [Route("GET", "/unicode")]
    public static HttpResponse unicodeTest(string line)
    {
        return HttpResponse.Ok(line);
    }
    
    [Response(200, "OK", typeof(User))]
    [Route("GET", "/user")]
    public static HttpResponse getUser()
    {
        return HttpResponse.Ok(JsonConvert.SerializeObject(new User { Name = "John", Age = 30 }));
    }

    public enum CalcMethod
    {
        Add,
        Multiply,
        Subtract,
        Divide
    }
    
    [Response(200, "Successfully calculated", typeof(float))]
    [Route("GET", "/calc/{method}/{a}/{b}", "/calc/{method}")]
    public static HttpResponse Calc(float a, float b, CalcMethod method)
    {
        switch (method)
        {
            case CalcMethod.Add:
                return HttpResponse.Ok((a+b).ToString(CultureInfo.InvariantCulture));
            case CalcMethod.Subtract:
                return HttpResponse.Ok((a-b).ToString(CultureInfo.InvariantCulture));
            case CalcMethod.Multiply:
                return HttpResponse.Ok((a*b).ToString(CultureInfo.InvariantCulture));
            case CalcMethod.Divide:
                return HttpResponse.Ok((a/b).ToString(CultureInfo.InvariantCulture));
        }
        return HttpResponse.Ok((a+b).ToString(CultureInfo.InvariantCulture));
    }
    
    [Route("GET", "/auth/code")]
    public static HttpResponse Auth(int x, float y, bool z, string str, Pipeline pipeline)
    {
        return pipeline.Authentification.SendAuthentification((x, y, z, str));
    }
    
    [Authentificated]
    [Route("GET", "/auth")]
    public static HttpResponse AuthCheck((int x, float y, bool z, string str) auth)
    {
        return HttpResponse.Ok(JsonConvert.SerializeObject(auth));
    }
    
    [Cached(60*10, 200)]
    [Route("GET", "/cache/{a}")]
    public static HttpResponse Cached(HttpRequest request, string a, string b)
    {
        if(a != "example")
            return HttpResponse.BadRequest(DateTime.UtcNow.ToString("G"));
        return HttpResponse.Ok(DateTime.UtcNow.ToString("G"));
    }
    [Cached(20, 200)]
    [Route("GET", "/cache/alt/{a}")]
    public static HttpResponse Cached2(string a)
    {
        return HttpResponse.Ok(DateTime.UtcNow.ToString("G"));
    }
    
    [Cached(60*60*24,200)]
    [Route("GET", "/image/{name}")]
    public static HttpResponse Image(string name)
    {
        return HttpResponse.File(name, "image/jpeg");
    }
}