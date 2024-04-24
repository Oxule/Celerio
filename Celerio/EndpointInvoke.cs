using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Celerio;

public class Parameter
{
    public static List<string> InternalNames = new (){"auth", "body"};
    public static List<Type> InternalTypes = new (){typeof(Pipeline), typeof(HttpRequest)};
        
    public string Name;
    public Type Type;
    public bool Required;
    public bool External;

    public Parameter(string name, Type type, bool required, bool external)
    {
        Name = name;
        Type = type;
        Required = required;
        External = external;
    }

    public Parameter(ParameterInfo parameter)
    {
        Name = parameter.Name ?? throw new ArgumentNullException("How tf you made parameter w/o name?");
        Type = parameter.ParameterType;
        Required = !parameter.HasDefaultValue;
        External = !(InternalNames.Contains(Name) || InternalTypes.Contains(Type));
    }
}

public class EndpointInvoke
{
    public class InvokeOverride
    {
        public Type Type { get; set; }
        public object Override { get; set; }
        public string Name { get; set; }

        public InvokeOverride(Type type, object @override, string name = "")
        {
            Type = type;
            Override = @override;
            Name = name;
        }
    }
    public delegate object? CustomDeserialize(Type type, string value);

    public List<CustomDeserialize> CustomDeserialization = new List<CustomDeserialize>();
    
    public HttpResponse Invoke(MethodInfo method, HttpRequest request,
        Dictionary<string, string> parameters, params InvokeOverride[] overrides)
    {
        var p = method.GetParameters();
        object?[] args = new object?[p.Length];

        for (int i = 0; i < p.Length; i++)
        {
            bool overriden = false;
            foreach (var ovr in overrides)
            {
                if (ovr.Name == "" || ovr.Name == p[i].Name)
                {
                    if (ovr.Type == p[i].ParameterType)
                    {
                        args[i] = ovr.Override;
                        overriden = true;
                        break;
                    }
                }
            }

            if (overriden)
                continue;

            var val = FindParameter(p[i].Name, request.Body, request.Query, parameters);
            if (val == null)
            {
                if (p[i].HasDefaultValue)
                {
                    args[i] = p[i].DefaultValue;
                    continue;
                }

                return new HttpResponse(400, "Bad Request", new Dictionary<string, string>(),
                    $"Parameter {p[i].Name.ToLower()} is not specified");
            }

            var minL = p[i].GetCustomAttribute<System.ComponentModel.DataAnnotations.MinLengthAttribute>();
            if (minL != null && val.Length < minL.Length)
                return new HttpResponse(400, "Bad Request", new Dictionary<string, string>(),
                    $"Parameter {p[i].Name.ToLower()} has minimal length {minL.Length}, but input length is {val.Length}");

            var maxL = p[i].GetCustomAttribute<System.ComponentModel.DataAnnotations.MaxLengthAttribute>();
            if (maxL != null && val.Length > maxL.Length)
                return new HttpResponse(400, "Bad Request", new Dictionary<string, string>(),
                    $"Parameter {p[i].Name.ToLower()} has maximal length {maxL.Length}, but input length is {val.Length}");

            var a = Deserialize(p[i].ParameterType, val);
            if (a == null)
            {
                if (p[i].HasDefaultValue)
                {
                    args[i] = p[i].DefaultValue;
                    continue;
                }

                return new HttpResponse(400, "Bad Request", new Dictionary<string, string>(),
                    $"Parameter {p[i].Name.ToLower()} is incorrect");
            }

            args[i] = a;
        }

        return (HttpResponse) method.Invoke(null, args);
    }

    private string? FindParameter(string key, string body, Dictionary<string, string> query,
        Dictionary<string, string> path)
    {
        var k = key.ToLower();
        if (k == "body")
            return body;

        foreach (var kvp in query)
        {
            if (kvp.Key.ToLower() == k)
                return kvp.Value;
        }

        foreach (var kvp in path)
        {
            if (kvp.Key.ToLower() == k)
                return kvp.Value;
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
        
        //Process Enums
        if (type.IsEnum)
        {
            var names = type.GetEnumNames();
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i].ToLower() == value.ToLower())
                {
                    return type.GetEnumValues().GetValue(i);
                }
            }
        }
        
        try
        {
            return JsonConvert.DeserializeObject(value, type);
        }
        catch
        {
            return null;
        }
    }
    
    public EndpointInvoke() { }
}