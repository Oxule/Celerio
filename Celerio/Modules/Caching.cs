using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Celerio;

public class Cached : Attribute
{
    public int Delay;
    public int[] StatusCodes;

    public Cached(int delaySeconds, params int[] statusCodes)
    {
        Delay = delaySeconds;
        StatusCodes = statusCodes;
        if (statusCodes.Length == 0)
            StatusCodes = new int[] {200};
    }
}

public class Caching : ModuleBase
{
    private class Cache
    {
        public HttpResponse? Response;
        public DateTime LastUpdated;
        public int UsedCount;
        public DateTime FirstSnapshot;

        public Cache(HttpResponse? response, DateTime lastUpdated, int usedCount, DateTime firstSnapshot)
        {
            Response = response;
            LastUpdated = lastUpdated;
            UsedCount = usedCount;
            FirstSnapshot = firstSnapshot;
        }

        public Cache(HttpResponse response)
        {
            Response = response;
            LastUpdated = DateTime.UtcNow;
            UsedCount = 0;
            FirstSnapshot = DateTime.UtcNow;
        }
    }

    private Dictionary<string, Cache> Caches = new Dictionary<string, Cache>();

    public int CacheLimitMin = 200;
    public int CacheLimitMax = 500;
    
    private string HashRequest(HttpRequest request)
    {
        var sb = new StringBuilder();
        sb.Append(request.Method);
        sb.Append(request.URI);
        sb.Append('?');
        foreach (var p in request.Query)
        {
            sb.Append(p.Key);
            sb.Append('=');
            sb.Append(p.Value);
            sb.Append('&');
        }

        return sb.ToString();
    }
    
    public override HttpResponse? BeforeEndpoint(Context context)
    {
        var attr = context.Endpoint!.Method.GetCustomAttribute<Cached>();
        if (attr != null)
        {
            var hash = HashRequest(context.Request);
            if (Caches.TryGetValue(hash, out var cache))
            {
                if (cache.LastUpdated.AddSeconds(attr.Delay) > DateTime.UtcNow)
                {
                    cache.UsedCount++;
                    //TODO: Did'nt work for now :(
                    /*
                    if (request.Headers.TryGetValue("If-Modified-Since", out var mod) && DateTime.TryParse(mod, out var modified))
                    {
                        if (modified > caches[hash].LastUpdated)
                        {
                            return new HttpResponse(304, "Not Modified", new Dictionary<string, string>(), "");
                        }
                    }*/
                    return cache.Response;
                }
            }
        }
        return null;
    }

    private void ClearCache()
    {
        foreach (var c in Caches.OrderBy(c=>c.Value.UsedCount/(DateTime.UtcNow-c.Value.FirstSnapshot).TotalSeconds).Take(Caches.Count-CacheLimitMin))
        {
            Caches.Remove(c.Key);
        }
    }
    
    public override HttpResponse? AfterEndpoint(Context context, HttpResponse response)
    {
        var attr = context.Endpoint!.Method.GetCustomAttribute<Cached>();
        if (attr != null)
        {
            if (attr.StatusCodes.Contains(response.StatusCode))
            {
                var hash = HashRequest(context.Request);
                if (Caches.TryGetValue(hash, out var cache))
                {
                    if (cache.LastUpdated.AddSeconds(attr.Delay) < DateTime.UtcNow)
                    {
                        cache.LastUpdated = DateTime.UtcNow;
                        cache.Response = response;
                    }
                }
                else
                {
                    if (Caches.Count > CacheLimitMax)
                    {
                        ClearCache();
                    }
                    Caches.Add(hash, new Cache(response, DateTime.UtcNow, 0, DateTime.UtcNow));
                }
            }
        }
        return null;
    }
}