using System.Reflection;

namespace Celerio;

public class Authentificated : Attribute { }

public class AuthentificatedCheck : IBeforeEndpoint
{
    public HttpResponse? BeforeEndpointHandler(HttpRequest request, EndpointRouter.Endpoint endpoint, Dictionary<string, string> parameters, Dictionary<string, string> auth)
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