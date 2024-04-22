using System.Reflection;

namespace Celerio;

public class EndpointRouter
{
    public class Endpoint
    {
        public Service? Service;
        public string[] Routes;
        public string HttpMethod;
        public MethodInfo? Info;

        public Endpoint(string[] routes, string httpMethod, MethodInfo? info, Service? service = null)
        {
            Routes = routes;
            HttpMethod = httpMethod;
            Info = info;
            Service = service;
        }
    }
    public readonly List<Endpoint> Endpoints = new List<Endpoint>();

    private PathMatcher PathMatcher { get; } = new ();
    
    public EndpointRouter()
    {
        Logging.Log("Searching For Endpoints...");
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var t in asm.GetTypes())
            {
                if(!t.IsClass)
                    continue;
                var service = t.GetCustomAttribute<Service>();
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

                    Endpoints.Add(new Endpoint(attr.URI,
                        attr.Method,
                        method, service));
                }
            }
        }
    }

    public Endpoint? GetEndpoint(HttpRequest request, out Dictionary<string,string>? parameters)
    { 
        parameters = null;
        foreach (var ep in Endpoints)
        {
            foreach (var r in ep.Routes)
            {
                if(request.Method != ep.HttpMethod)
                    continue;
                if(PathMatcher.Match(request.URI, r, out parameters))
                    return ep;
            }
        }
        return null;
    }
}