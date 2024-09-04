namespace Celerio;

public class CORS
{
    public static readonly string[] DefaultMethods = { "GET", "POST", "PUT", "DELETE", "OPTIONS", "HEAD", "PATCH" };
    
    //Access-Control-Allow-Origin
    private List<string> AllowedOrigins = new ();
    
    private string[] AllowedMethods = DefaultMethods;

    private string[] AllowedHeaders = {"*"};

    //Access-Control-Allow-Credentials
    private bool Credentials = false;

    public CORS SetMethods(params string[] methods)
    {
        AllowedMethods = methods;
        return this;
    }
    public CORS SetHeaders(params string[] headers)
    {
        AllowedHeaders = headers;
        return this;
    }

    public CORS AddOrigin(string origin)
    {
        AllowedOrigins.Add(origin);
        return this;
    }

    public CORS AllowCredentials(bool allow)
    {
        Credentials = allow;
        return this;
    }
    
    public void AddHeaders(HeadersCollection headers, string? origin = null)
    {
        headers.Add("Access-Control-Allow-Credentials", Credentials.ToString().ToLower());

        if (AllowedOrigins.Count > 0)
        {
            string preferOrigin = AllowedOrigins[0];
            if (origin != null && AllowedOrigins.Contains(origin))
                preferOrigin = origin;
            
            headers.Add("Access-Control-Allow-Origin", preferOrigin);
        }

        headers.Add("Access-Control-Allow-Headers", string.Join(", ", AllowedHeaders));
        headers.Add("Access-Control-Allow-Methods", string.Join(", ", AllowedMethods));
    }
}

public static class CORSExtention
{
    public static HttpResponse AddCorsHeaders(this HttpResponse resp, CORS cors, string? origin = null)
    {
        cors.AddHeaders(resp.Headers, origin);
        return resp;
    }
}