using System.Reflection;

namespace Celerio;

public class Endpoint
{
    public string HttpMethod;
    public RoutePattern Route;

    public object? Target = null;
    public MethodInfo Method;

    public Arguments Arguments;

    public struct RoutePattern
    {
        internal readonly string[] _parts;
        internal readonly bool[] _dynamic;
        public readonly string[] DynamicParameters;
        public readonly string Route;

        public RoutePattern(string pattern)
        {
            Route = pattern;
            _parts = pattern.Split('/', StringSplitOptions.RemoveEmptyEntries);
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

    public Endpoint(string httpMethod, string route, Delegate action) 
    {
        HttpMethod = httpMethod;
        Route = new RoutePattern(route);
        Target = action.Target;
        Method = action.Method;
    }
}