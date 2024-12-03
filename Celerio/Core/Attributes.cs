namespace Celerio;

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