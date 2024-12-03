using System.Reflection;
using System.Text;
using Celerio.InvokeModules;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Celerio;

public class EndpointInvoker
{
    internal HttpResponse CallEndpoint(Context context)
    {
        if (!context.Endpoint!.Arguments.Resolve(context, out var args, out var reason))
            return HttpResponse.BadRequest(reason);
        
        object? resp;
        try
        {
            resp = context.Endpoint.Method.Invoke(context.Endpoint!.Target, args);
        }
        catch (Exception e)
        {
            return HttpResponse.InternalServerError(e.ToString());
        }
        return ResponseResolver.Resolve(resp);
    }
}