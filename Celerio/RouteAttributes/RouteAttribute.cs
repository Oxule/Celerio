namespace Celerio;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class RouteAttribute : Attribute
{
    public string Method { get; }
    public string Pattern { get; }

    public RouteAttribute(string method, string pattern)
    {
        Method = method;
        Pattern = pattern;
    }
} 