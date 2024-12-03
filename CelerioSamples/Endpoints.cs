using System.Globalization;
using Celerio;
using Newtonsoft.Json;

namespace CelerioSamples;

public static class Endpoints
{
    [Route("GET", "/")]
    public static HttpResponse Index()
    {
        return HttpResponse.Ok("Hello! This is Celerio Sample!");
    }
    
    [Route("GET", "/ping")]
    public static HttpResponse Ping()
    {
        return HttpResponse.Ok("");
    }
    
    [Route("GET", "/echo")]
    public static HttpResponse Ping(string message)
    {
        return HttpResponse.Ok(message);
    }
    
    public enum SampleEnum
    {
        A,
        B
    }
    [Route("GET", "/enum")]
    public static string EnumTest(SampleEnum e)
    {
        switch (e)
        {
            case SampleEnum.A:
                return "It's A enum";
            case SampleEnum.B:
                return "It's B enum";
        }
        return "Awwww..";
    }
    
    [Route("GET", "/sum")]
    public static int Sum(int a, int b)
    {
        return a+b;
    }
    
    [Route("GET", "/force")]
    public static float Force(float mass, float g = 10)
    {
        return mass*g;
    }

    [Route("POST", "/form-data")]
    public static object? FormData(MultipartData body)
    {
        return body;
    }
    
    [Route("GET", "/path/{id}/{name}")]
    public static (int id, string name) Path(int id, string name)
    {
        return (id, name);
    }
    
    public record User
    {
        public string Name;
        public int Age;
    }
    
    [Route("POST", "/user")]
    public static User user(User body)
    {
        return body;
    }

    [Route("GET", "/custom")]
    public static HttpResponse CustomResponse()
    {
        return new HttpResponse(228, "Super Custom Response").SetBody("Super Puper Mega Duper Custom Body")
            .AddHeader("Custom-Header", "Custom Value 1").AddHeader("Custom-Header", "Custom Value 2");
    }
    
    [Route("GET", "/unicode")]
    public static string unicodeTest(string line)
    {
        return line;
    }
    
    [Route("GET", "/user")]
    public static User getUser()
    {
        return new User { Name = "John", Age = 30 };
    }

    public enum CalcMethod
    {
        Add,
        Multiply,
        Subtract,
        Divide
    }
    
    [Route("GET", "/calc/{method}")]
    public static float Calc(float a, float b, CalcMethod method)
    {
        switch (method)
        {
            case CalcMethod.Add:
                return a+b;
            case CalcMethod.Subtract:
                return a-b;
            case CalcMethod.Multiply:
                return a*b;
            case CalcMethod.Divide:
                return a/b;
        }
        return 54;
    }
    
    [Cached(20, 200)]
    [Route("GET", "/cache")]
    public static HttpResponse Cached()
    {
        return HttpResponse.Ok(DateTime.UtcNow.ToString("G"));
    }
}