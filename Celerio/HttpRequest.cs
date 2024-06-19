namespace Celerio;

public class HttpRequest
{
    public string Method { get; set; }
    public string URI { get; set; }
    public Dictionary<string, string> Query { get; set; }
    public Dictionary<string, string> Headers { get; set; }
    public byte[]? BodyRaw { get; set; }
    public string? Body { get; set; }

    public string? GetCookie(string key)
    {
        if (!Headers.TryGetValue("Cookie", out var cookie))
            return null;

        foreach (var c in cookie.Split("; "))
        {
            if(c.Length <= key.Length)
                continue;
            for (int i = 0; i < key.Length; i++)
            {
                if (c[i] != key[i])
                    goto no;
            }

            if (c[key.Length] == '=')
            {
                var offset = key.Length + 1;
                var value = c.Substring(offset, c.Length - offset);
                return value;
            }
            
            no:
            continue;
        }
        
        return null;
    }
    
    public HttpRequest()
    {
        Query = new Dictionary<string, string>();
        Headers = new Dictionary<string, string>();
    }
}