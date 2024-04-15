using System.Reflection;

namespace Celerio;


[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class Route : Attribute
{
    public string Method { get; set; }
    public string[] URI { get; set; }

    public Route(string method, params string[] uri)
    {
        Method = method;
        URI = uri;
    }
} 

public class EndpointRouter
{
    public class Endpoint
    {
        public string[] Routes;
        public string HttpMethod;
        public MethodInfo? Info;

        public Endpoint(string[] routes, string httpMethod, MethodInfo? info)
        {
            Routes = routes;
            HttpMethod = httpMethod;
            Info = info;
        }
    }
    private List<Endpoint> endpoints = new List<Endpoint>();

    private PathMatcher PathMatcher { get; } = new ();
    
    public EndpointRouter()
    {
        Logging.Log("Searching For Endpoints...");
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var t in asm.GetTypes())
            {
                foreach (var method in t.GetMethods())
                {
                    if (!method.IsStatic)
                        continue;
                    var attr = method.GetCustomAttribute<Route>();
                    if (attr == null)
                        continue;
                    if(method.ReturnType != typeof(HttpResponse))
                        continue;
                    
                    Logging.Log($"Found Endpoint: {attr.Method} {string.Join(" ", attr.URI)}");

                    endpoints.Add(new Endpoint(attr.URI,
                        attr.Method,
                        method));
                }
            }
        }
    }

    public Endpoint? GetEndpoint(HttpRequest request, out Dictionary<string,string>? parameters)
    {
        parameters = null;
        foreach (var ep in endpoints)
        {
            foreach (var r in ep.Routes)
            {
                if(PathMatcher.Match(request.URI, r, out parameters))
                    return ep;
            }
        }
        return null;
    }
    
    
}