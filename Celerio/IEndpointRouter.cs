namespace Celerio;

public interface IEndpointRouter
{
    
}

public class Endpoint
{
    public delegate HttpResponse EndpointDelegate(HttpRequest request);
    public EndpointDelegate Method;
    public string[] Routes;
}

public class EndpointRouter : IEndpointRouter
{
    public interface IPathMatcher
    {
        public bool Match(string path, string pattern, out string[] parameters);
    }

    public class PathMatcher : IPathMatcher
    {
        public bool Match(string path, string pattern, out string[] parameters)
        {
            parameters = new string[]{};
            
            return true;
        }
    }
}