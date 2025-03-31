using System.Reflection;
using System.Text.RegularExpressions;

namespace Celerio;

public class EndpointManager
{
    public void Map(string method, string route, Delegate action)
    {
        var ep = new Endpoint(method, route, action);
        ep.Arguments = Arguments.GetArguments(ep);
        _endpoints.Add(ep);
    }

    
    
    public Endpoint? GetEndpoint(HttpRequest request, out string[] pathParameters)
    {
        if (_tree.TryMatch(request.URI, out var ep, out pathParameters))
            return ep;
        
        pathParameters = Array.Empty<string>();
        return null;
    }

    public HttpResponse CallEndpoint(Context context) => _invoker.CallEndpoint(context);
    
    private List<Endpoint> _endpoints = new ();
    private EndpointsTree _tree;

    private EndpointInvoker _invoker = new();

    internal void MapStatic()
    {
        Logging.Log("Searching for endpoints...");
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var t in asm.GetTypes())
            {
                if(!t.IsClass)
                    continue;
                foreach (var method in t.GetMethods())
                {
                    if (!method.IsStatic)
                        continue;
                    var attr = method.GetCustomAttribute<Route>();
                    if (attr == null)
                        continue;
                    
                    Logging.Log($"Found endpoint: {attr.Method} {attr.Pattern}");

                    var ep = new Endpoint(attr.Method, attr.Pattern, method);
                    ep.Arguments = Arguments.GetArguments(ep);
                    _endpoints.Add(ep);
                }
            }
        }
    }

    internal void BuildTree()
    {
        _tree = EndpointsTree.BuildTree(_endpoints);
    }
}