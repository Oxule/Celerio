using System.ComponentModel;
using System.Reflection;

namespace Celerio;


public class MethodInvoke
{
    public delegate object? CustomDeserialize(Type type, string value);
    public List<CustomDeserialize> CustomDeserialization = new List<CustomDeserialize>();
    
    public HttpResponse ParameterizedInvoke(MethodInfo method, HttpRequest request,
        Dictionary<string, string> parameters)
    {
        var p = method.GetParameters();
        object?[] args = new object?[p.Length];

        for (int i = 0; i < p.Length; i++)
        {
            if (p[i].ParameterType == typeof(HttpRequest))
                args[i] = request;
            else
            {
                var val = FindParameter(p[i].Name, request.Query, parameters, request.Headers);
                if (val == null)
                {
                    if (p[i].HasDefaultValue)
                    {
                        args[i] = p[i].DefaultValue;
                        continue;
                    }
                        
                    return new HttpResponse(400, "Bad Request", new Dictionary<string, string>(), $"Parameter {p[i].Name.ToLower()} is not specified");
                }

                var a = Deserialize(p[i].ParameterType, val);
                if (a == null)
                {
                    if (p[i].HasDefaultValue)
                    {
                        args[i] = p[i].DefaultValue;
                        continue;
                    }
                    return new HttpResponse(400, "Bad Request", new Dictionary<string, string>(), $"Parameter {p[i].Name.ToLower()} is incorrect");
                }
                
                args[i] = a;
            }
        }
        
        return (HttpResponse)method.Invoke(null, args);
    }

    private string? FindParameter(string key, params Dictionary<string, string>[] dictionaries)
    {
        var k = key.ToLower();
        foreach (var dictionary in dictionaries)
        {
            foreach (var kvp in dictionary)
            {
                if(kvp.Key.ToLower() == k)
                    return kvp.Value;
            }
        }

        return null;
    } 
    
    public object? Deserialize(Type type, string value)
    {
        foreach (CustomDeserialize d in CustomDeserialization)
        {
            var obj = d.Invoke(type, value);
            if (obj != null)
                return obj;
        }

        if (type == typeof(string))
            return value;

        try
        {
            TypeConverter conv = TypeDescriptor.GetConverter(type);
            return conv.ConvertFromInvariantString(value);
        }
        catch
        {
            return null;
        }
    }
}