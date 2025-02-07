namespace Celerio;

public class HttpRequest
{
    public string Method { get; set; }
    public string URI { get; set; }
    public Dictionary<string, string> Query { get; set; } = new();
    public HeadersCollection Headers { get; set; } = new();
    public byte[]? Body { get; set; }
}