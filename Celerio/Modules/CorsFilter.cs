namespace Celerio;

public class CorsFilter : ModuleBase
{
    public override HttpResponse? AfterRequest(Context context)
    {
        if (context.Request.Method == "OPTIONS")
            return HttpResponse.Ok("").AddCorsHeaders(context.Pipeline.Cors);
        return null;
    }

    public override HttpResponse? AfterEndpoint(Context context, HttpResponse response)
    {
        response.AddCorsHeaders(context.Pipeline.Cors);
        return null;
    }
}