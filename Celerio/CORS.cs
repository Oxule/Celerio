namespace Celerio;

public class CORS
{
    //Access-Control-Allow-Origin
    public List<string> Allowed = new ();

    public void AddHeaders(HeadersCollection headers)
    {
        if(Allowed.Count == 0)
            headers.Add("Access-Control-Allow-Origin", "None");
        else
            headers.Add("Access-Control-Allow-Origin", string.Join(", ", Allowed.ToArray()));
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