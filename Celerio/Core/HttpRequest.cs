namespace Celerio;

public class HttpRequest
{
    public string Method { get; set; }
    public string URI { get; set; }
    public Dictionary<string, string> Query { get; set; }
    public HeadersCollection Headers { get; set; }
    public byte[]? BodyRaw { get; set; }
    public string? Body { get; set; }
    
    public HttpRequest()
    {
        Query = new Dictionary<string, string>();
        Headers = new ();
    }
}