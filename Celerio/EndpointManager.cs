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
            private string[] _parts;
            private bool[] _dynamic;
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
        if (action.Method.IsStatic)
            throw new Exception("Every Endpoint must be static!");
        _endpoints.Add(new (method, route, action.Method));
    }

    public void MapGet(string route, Delegate action) => Map("GET", route, action);
    public void MapPost(string route, Delegate action) => Map("POST", route, action);
    public void MapDelete(string route, Delegate action) => Map("DELETE", route, action);
    public void MapPut(string route, Delegate action) => Map("PUT", route, action);

    public Endpoint? GetEndpoint(HttpRequest request, out string[] pathParameters)
    {
        pathParameters = Array.Empty<string>();
        foreach (var ep in _endpoints)
        {
            if (ep.HttpMethod != request.Method)
                continue;
            if (Endpoint.RoutePattern.Match(ep.Route, request.URI, out pathParameters))
            {
                return ep;
            }
        }

        return null;
    }

    public HttpResponse CallEndpoint(Context context, string[] pathParameters)
    {
        var parameters = context.Endpoint.Method.GetParameters();
        object?[] args = new object?[parameters.Length];
        for (int i = 0; i < args.Length; i++)
        {
            try
            {
                if (parameters[i].ParameterType == typeof(Context))
                {
                    args[i] = context;
                    continue;
                }

                string param;
                
                var path = GetPathParameter(parameters[i].Name, context.Endpoint.Route.DynamicParameters, pathParameters);
                if (path != null)
                {
                    param = path;
                    goto paramFound;
                }
                if (context.Request.Query.TryGetValue(parameters[i].Name, out var query))
                {
                    param = query;
                    goto paramFound;
                }
                if (parameters[i].HasDefaultValue)
                {
                    args[i] = parameters[i].DefaultValue;
                    continue;
                }
                if (parameters[i].Name == "body")
                {
                    if(string.IsNullOrEmpty(context.Request.Body))
                        return HttpResponse.BadRequest("Body is empty");
                    param = context.Request.Body;
                    goto paramFound;
                }
                return HttpResponse.BadRequest($"Parameter {parameters[i].Name} didn't specified!");

                paramFound:
                
                if (parameters[i].ParameterType == typeof(string))
                    args[i] = param;
                else
                    args[i] = JsonConvert.DeserializeObject(param, parameters[i].ParameterType);
            }
            catch (Exception e)
            {
                return HttpResponse.BadRequest(
                    $"Unknown parameter error at {parameters[i].Name}({parameters[i].ParameterType})");
            }
        }
        
        try
        {
            var respRaw = context.Endpoint.Method.Invoke(null, args);
            HttpResponse resp;
            if (respRaw.GetType() == typeof(HttpResponse))
                resp = (HttpResponse)respRaw;
            else
                resp = HttpResponse.Ok(JsonConvert.SerializeObject(respRaw));

            return resp;
        }
        catch (Exception e)
        {
            return HttpResponse.InternalServerError(e.Message+'\n'+e.StackTrace);
        }
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
    
    private List<Endpoint> _endpoints = new List<Endpoint>();

    public EndpointManager()
    {
        Logging.Log("Searching For Endpoints...");
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
                    
                    Logging.Log($"Found Endpoint: {attr.Method} {attr.Pattern}");

                    _endpoints.Add(new (attr.Method, attr.Pattern, method));
                }
            }
        }
    }
}