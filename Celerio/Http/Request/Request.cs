namespace Celerio;

public class Request
{
    public string Method;
    public string Path;
    public Dictionary<string, string> Query;
    public HeaderCollection Headers;
    public byte[] Body;

    public Request(string method, string path, Dictionary<string, string> query, HeaderCollection headers, byte[] body)
    {
        Method = method;
        Path = path;
        Query = query;
        Headers = headers;
        Body = body;
    }
}