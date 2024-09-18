using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Celerio;

public class EndpointManager
{
    public class Endpoint
    {
        public string HttpMethod;
        public RoutePattern Route;
        public MethodInfo Method;

        public struct RoutePattern
        {
            private readonly string[] _parts;
            private readonly bool[] _dynamic;
            public string[] DynamicParameters;

            public RoutePattern(string pattern)
            {
                _parts = pattern.Split('/');
                _dynamic = new bool[_parts.Length];
                var dynamicParams = new List<string>(_parts.Length);
                for (int i = 0; i < _parts.Length; i++)
                {
                    if (_parts[i].Length >= 2 && _parts[i][0] == '{' && _parts[i][^1] == '}')
                    {
                        _parts[i] = _parts[i].Substring(1, _parts[i].Length - 2);
                        _dynamic[i] = true;
                        dynamicParams.Add(_parts[i]);
                    }
                }

                DynamicParameters = dynamicParams.ToArray();
            }
            
            public static bool Match(RoutePattern pattern, string path, out string[] dynamicValues)
            {
                dynamicValues = new string[pattern.DynamicParameters.Length];
                var p = path.Split('/');
                if (p.Length != pattern._parts.Length)
                    return false;
                int d = 0;
                for (int i = 0; i < p.Length; i++)
                {
                    if (pattern._dynamic[i])
                    {
                        dynamicValues[d] = p[i];
                        d++;
                    }
                    else if (p[i] != pattern._parts[i])
                        return false;
                }
                return true;
            }
        }

        public Endpoint(string httpMethod, RoutePattern route, MethodInfo method)
        {
            HttpMethod = httpMethod;
            Route = route;
            Method = method;
        }
        
        public Endpoint(string httpMethod, string route, MethodInfo method)
        {
            HttpMethod = httpMethod;
            Route = new RoutePattern(route);
            Method = method;
        }
    }

    public void Map(string method, string route, Delegate action)
    {
        if (!action.Method.IsStatic)
            throw new Exception("Every endpoint must be static!");
        _endpoints.Add(new (method, route, action.Method));
    }

    public Endpoint? GetEndpoint(HttpRequest request, out string[] pathParameters)
    {
        foreach (var ep in _endpoints)
        {
            if (ep.HttpMethod != request.Method)
                continue;
            if (Endpoint.RoutePattern.Match(ep.Route, request.URI, out pathParameters))
            {
                return ep;
            }
        }

        pathParameters = Array.Empty<string>();
        return null;
    }

    public HttpResponse CallEndpoint(Context context, string[] pathParameters)
    {
        var parameters = context.Endpoint!.Method.GetParameters();
        object?[] args = new object?[parameters.Length];
        for (int i = 0; i < args.Length; i++)
        {
            if (parameters[i].ParameterType == typeof(Context))
                {
                    args[i] = context;
                    continue;
                }
            
            string value;
                
            var path = GetPathParameter(parameters[i].Name!, context.Endpoint.Route.DynamicParameters, pathParameters);
            if (path != null)
            {
                value = path;
            }
            else if (context.Request.Query.TryGetValue(parameters[i].Name!, out var query))
            {
                value = query;
            }
            else if (parameters[i].HasDefaultValue)
            {
                args[i] = parameters[i].DefaultValue;
                continue;
            }
            else if (parameters[i].Name == "body")
            {
                if(string.IsNullOrEmpty(context.Request.Body))
                    return HttpResponse.BadRequest("Body is empty");
                value = context.Request.Body;
            }
            else
                return HttpResponse.BadRequest($"Parameter {parameters[i].Name} didn't specified!");
                
            if (parameters[i].ParameterType == typeof(string))
                args[i] = value;
            else
            {
                try
                {
                    args[i] = JsonConvert.DeserializeObject(value, parameters[i].ParameterType);
                }
                catch (Exception e)
                {
                    return HttpResponse.BadRequest($"Parameter {parameters[i].Name} incorrect type");
                }
            }
        }

        object? respRaw;
        
        try
        {
            respRaw = context.Endpoint.Method.Invoke(null, args);
        }
        catch (Exception e)
        {
            return HttpResponse.InternalServerError(e.ToString());
        }
        HttpResponse resp;
        
        if (respRaw != null && respRaw.GetType() == typeof(HttpResponse))
            resp = (HttpResponse)respRaw;
        else
            resp = HttpResponse.Ok(JsonConvert.SerializeObject(respRaw));
        return resp;
    }
    
    private string? GetPathParameter(string key, string[] keys, string[] values)
    {
        for (int i = 0; i < keys.Length; i++)
        {
            if (keys[i] == key)
                return values[i];
        }

        return null;
    }
    
    private List<Endpoint> _endpoints = new ();

    public EndpointManager()
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

                    _endpoints.Add(new (attr.Method, attr.Pattern, method));
                }
            }
        }
    }
}