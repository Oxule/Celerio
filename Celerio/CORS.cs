namespace Celerio;

public class CORS
{
    //Access-Control-Allow-Origin
    public List<string> AllowedOrigins = new ();

    //Access-Control-Allow-Credentials
    public bool Credentials = false;

    public CORS AddOrigin(string origin)
    {
        AllowedOrigins.Add(origin);
        return this;
    }
    
    public void AddHeaders(HeadersCollection headers)
    {
        
        headers.Add("Access-Control-Allow-Credentials", Credentials.ToString().ToLower());
        if(AllowedOrigins.Count == 0)
            headers.Add("Access-Control-Allow-Origin", "None");
        else
            headers.Add("Access-Control-Allow-Origin", string.Join(", ", AllowedOrigins.ToArray()));
        
        headers.Add("Access-Control-Allow-Headers", "*");
        headers.Add("Access-Control-Allow-Methods", "*");
    }
}

public static class CORSExtention
{
    public static HttpResponse AddCorsHeaders(this HttpResponse resp, CORS cors)
    {
        cors.AddHeaders(resp.Headers);
        return resp;
    }
}