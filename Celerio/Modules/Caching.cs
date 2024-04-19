using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Celerio;

public class Cached : Attribute
{
    public int Delay;

    public Cached(int delay)
    {
        Delay = delay;
    }
}

public class Caching : IBeforeEndpoint, IAfterEndpoint
{
    private class Cache
    {
        public HttpResponse Response;
        public DateTime LastUpdated;

        public Cache(HttpResponse response, DateTime lastUpdated)
        {
            Response = response;
            LastUpdated = lastUpdated;
        }

        public Cache(HttpResponse response)
        {
            Response = response;
            LastUpdated = DateTime.UtcNow;
        }
    }

    private Dictionary<string, Cache> caches = new Dictionary<string, Cache>();

    private string HashRequest(HttpRequest request)
    {
        return request.Method+request.URI;
    }
    
    public HttpResponse? BeforeEndpointHandler(HttpRequest request, EndpointRouter.Endpoint endpoint, Dictionary<string, string> parameters, Dictionary<string, string> auth, Pipeline pipeline)
    {
        var attr = endpoint.Info.GetCustomAttribute<Cached>();
        if (attr != null)
        {
            var hash = HashRequest(request);
            if (!caches.ContainsKey(hash))
                caches.Add(hash, new Cache(null, DateTime.MinValue));

            if (caches[hash].LastUpdated.AddSeconds(attr.Delay) > DateTime.UtcNow)
            {
                //Did'nt work for now :(
                /*
                if (request.Headers.TryGetValue("If-Modified-Since", out var mod) && DateTime.TryParse(mod, out var modified))
                {
                    if (modified > caches[hash].LastUpdated)
                    {
                        return new HttpResponse(304, "Not Modified", new Dictionary<string, string>(), "");
                    }
                }*/
                return caches[hash].Response;
            }
        }
        return null;
    }

    public HttpResponse? AfterEndpointHandler(HttpRequest request, EndpointRouter.Endpoint endpoint, Dictionary<string, string> parameters, Dictionary<string, string> auth,
        Pipeline pipeline, HttpResponse response)
    {
        var attr = endpoint.Info.GetCustomAttribute<Cached>();
        if (attr != null)
        {
            var hash = HashRequest(request);

            if (caches[hash].LastUpdated.AddSeconds(attr.Delay) < DateTime.UtcNow)
            {
                caches[hash].LastUpdated = DateTime.UtcNow;
                caches[hash].Response = response;
            }
        }
        return null;
    }
}