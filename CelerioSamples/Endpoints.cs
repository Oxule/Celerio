﻿using Celerio;
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
                return HttpResponse.Ok((a+b).ToString());
            case CalcMethod.Subtract:
                return HttpResponse.Ok((a-b).ToString());
            case CalcMethod.Multiply:
                return HttpResponse.Ok((a*b).ToString());
            case CalcMethod.Divide:
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
    
    [Cached(60*60*24)]
    [Route("GET", "/image/{name}")]
    public static HttpResponse Image(string name)
    {
        return HttpResponse.File(name, "image/jpeg");
    }
}