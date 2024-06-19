using System.Reflection;

namespace Celerio;

public class Authentificated : Attribute { }

public class AuthentificatedCheck : ModuleBase
{
    public override HttpResponse? BeforeEndpoint(HttpRequest request, EndpointRouter.Endpoint endpoint, Dictionary<string, string> parameters, object? auth, Pipeline pipeline)
    {
        var attr = endpoint.Info.GetCustomAttribute<Authentificated>();
        if (attr != null)
        {
            if (auth == null)
                return HttpResponse.Unauthorized();
        }
        return null;
    }
}