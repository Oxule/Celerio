using System.Reflection;

namespace Celerio;

public class Authentificated : Attribute { }

public class AuthentificatedCheck : ModuleBase
{
    public override HttpResponse? BeforeEndpoint(Context context)
    {
        var attr = context.Endpoint!.Method.GetCustomAttribute<Authentificated>();
        if (attr != null)
        {
            if (context.Identity == null)
                return HttpResponse.Unauthorized();
        }
        return null;
    }
}