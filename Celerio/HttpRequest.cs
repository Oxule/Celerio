namespace Celerio;

public class HttpRequest
{
    public string Method { get; set; }
    public string URI { get; set; }
    public Dictionary<string, string> Query { get; set; }
    public Dictionary<string, string> Headers { get; set; }
    public string? Body { get; set; }

    public string? GetCookie(string key)
    {
        if (!Headers.TryGetValue("Cookie", out var cookie))
            return null;

        foreach (var c in cookie.Split("; "))
        {
            var p = c.Split("=");
            if (p.Length == 2)
            {
                if (p[0] == key)
                    return p[1];
            }
        }
        
        return null;
    }
    
    public HttpRequest(string method, string uri, Dictionary<string, string> query, Dictionary<string, string> headers, string? body)
    {
        Method = method;
        URI = uri;
        Query = query;
        Headers = headers;
        Body = body;
    }

    public HttpRequest()
    {
        Query = new Dictionary<string, string>();
        Headers = new Dictionary<string, string>();
    }
}