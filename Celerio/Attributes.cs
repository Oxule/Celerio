namespace Celerio;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public class Response : Attribute
{
    public Type? Type { get; set; }
    public int StatusCode { get; set; }
    public string Description { get; set; }

    public Response(int statusCode, string description, Type? type = null)
    {
        Type = type;
        StatusCode = statusCode;
        Description = description;
    }
} 

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class Route : Attribute
{
    public string Method { get; }
    public string Pattern { get; }

    public Route(string method, string pattern)
    {
        Method = method;
        Pattern = pattern;
    }
} 