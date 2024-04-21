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
    public string Method { get; set; }
    public string[] URI { get; set; }

    public Route(string method, params string[] uri)
    {
        Method = method;
        URI = uri;
    }
} 

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class Service : Attribute
{
    public string Name { get; set; }
    public string Description { get; set; }

    public Service(string name, string description = "")
    {
        Name = name;
        Description = description;
    }
} 