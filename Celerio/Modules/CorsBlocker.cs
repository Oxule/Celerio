namespace Celerio;

public class CorsBlocker : ModuleBase
{
    public override HttpResponse? AfterRequest(Context context)
    {
        if (context.Request.Method == "OPTIONS")
            return HttpResponse.Ok("").AddCorsHeaders(context.Pipeline.Cors);
        
        if (context.Pipeline.Cors.Allowed.Contains("*"))
            return null;
        if (context.Request.Headers.TryGet("Origin", out var v))
        {
            if (context.Pipeline.Cors.Allowed.Contains(v[0]))
                return null;
        }
        return HttpResponse.Forbidden().AddCorsHeaders(context.Pipeline.Cors);
    }

    public override HttpResponse? AfterEndpoint(Context context, HttpResponse response)
    {
        response.AddCorsHeaders(context.Pipeline.Cors);
        return null;
    }
}