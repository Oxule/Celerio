namespace Celerio;

public class CorsFilter : ModuleBase
{
    private readonly Cors _configuration;
    
    public override HttpResponse? AfterRequest(Context context)
    {
        if (context.Request.Method == "OPTIONS")
            return HttpResponse.Ok().AddCorsHeaders(_configuration, context.Request.Headers.GetFirst("Origin"));
        return null;
    }

    public override HttpResponse? AfterEndpoint(Context context, HttpResponse response)
    {
        response.AddCorsHeaders(_configuration, context.Request.Headers.GetFirst("Origin"));
        return null;
    }

    public CorsFilter(Cors configuration)
    {
        _configuration = configuration;
    }
}

public class Cors 
{
    public static readonly string[] DefaultMethods = { "GET", "POST", "PUT", "DELETE", "OPTIONS", "HEAD", "PATCH" };
    
    //Access-Control-Allow-Origin
    private List<string> AllowedOrigins = new ();
    
    private string[] AllowedMethods = DefaultMethods;

    private string[] AllowedHeaders = {"*"};

    //Access-Control-Allow-Credentials
    private bool Credentials = false;

    public Cors SetMethods(params string[] methods)
    {
        AllowedMethods = methods;
        return this;
    }
    public Cors SetHeaders(params string[] headers)
    {
        AllowedHeaders = headers;
        return this;
    }

    public Cors AddOrigin(string origin)
    {
        AllowedOrigins.Add(origin);
        return this;
    }

    public Cors AllowCredentials(bool allow)
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

public static class CorsExtention 
{
    public static HttpResponse AddCorsHeaders(this HttpResponse resp, Cors cors, string? origin = null)
    {
        cors.AddHeaders(resp.Headers, origin);
        return resp;
    }

    public static Pipeline ConfigureCors(this Pipeline pipeline,Cors cors)
    {
        return pipeline.AddModule(new CorsFilter(cors), true);
    }
}