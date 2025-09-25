namespace Celerio;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class GetAttribute : Attribute
{
    public string Pattern { get; }

    public GetAttribute(string pattern)
    {
        Pattern = pattern;
    }
}
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class PostAttribute : Attribute
{
    public string Pattern { get; }

    public PostAttribute(string pattern)
    {
        Pattern = pattern;
    }
} 
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class PutAttribute : Attribute
{
    public string Pattern { get; }

    public PutAttribute(string pattern)
    {
        Pattern = pattern;
    }
} 
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class PatchAttribute : Attribute
{
    public string Pattern { get; }

    public PatchAttribute(string pattern)
    {
        Pattern = pattern;
    }
} 
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class DeleteAttribute : Attribute
{
    public string Pattern { get; }

    public DeleteAttribute(string pattern)
    {
        Pattern = pattern;
    }
} 