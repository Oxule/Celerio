using System.Reflection;

namespace Celerio;


public class EndpointRouter
{
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
    
    public class Endpoint
    {
        public delegate HttpResponse EndpointDelegate(HttpRequest request);
        public EndpointDelegate Method;
        public string[] Routes;
        public string HttpMethod;
        public MethodInfo? Info;

        public Endpoint(EndpointDelegate method, string[] routes, string httpMethod, MethodInfo? info)
        {
            Method = method;
            Routes = routes;
            HttpMethod = httpMethod;
            Info = info;
        }
    }
    public List<Endpoint> Endpoints = new List<Endpoint>();

    private IPathMatcher PathMatcher { get; }
    
    public EndpointRouter()
    {
        PathMatcher = new PathMatcher();
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

                    Logging.Log($"Found Endpoint: {attr.Method} {attr.URI[0]}");

                    Endpoints.Add(new Endpoint(method.CreateDelegate<Endpoint.EndpointDelegate>(), attr.URI,
                        attr.Method,
                        method));
                }
            }
        }
    }

    public Endpoint? GetRoute(HttpRequest request)
    {
        foreach (var ep in Endpoints)
        {
            foreach (var r in ep.Routes)
            {
                if(PathMatcher.Match(request.URI, r, out var p))
                    return ep;
            }
        }
        return null;
    }
}