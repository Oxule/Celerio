using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Celerio;


public class MethodInvoke
{
    public delegate object? CustomDeserialize(Type type, string value);
    public List<CustomDeserialize> CustomDeserialization = new List<CustomDeserialize>();
    
    public class InvokeOverride
    {
        public Type Type { get; set; }
        public object Override { get; set; }
        public string Name { get; set; }

        public InvokeOverride(Type type, object @override, string name)
        {
            Type = type;
            Override = @override;
            Name = name;
        }
    }
    
    public HttpResponse ParameterizedInvoke(MethodInfo method, HttpRequest request,
        Dictionary<string, string> parameters, params InvokeOverride[] overrides)
    {
        var p = method.GetParameters();
        object?[] args = new object?[p.Length];

        for (int i = 0; i < p.Length; i++)
        {
            if (p[i].Name.ToLower() == "body")
            {
                if (request.Body != null)
                {
                    args[i] = Deserialize(p[i].ParameterType, request.Body);
                    continue;
                }
            }
            
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
            if(overriden)
                continue;
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
            
            var minL = p[i].GetCustomAttribute<System.ComponentModel.DataAnnotations.MinLengthAttribute>();
            if(minL != null && val.Length < minL.Length)
                return new HttpResponse(400, "Bad Request", new Dictionary<string, string>(), $"Parameter {p[i].Name.ToLower()} has minimal length {minL.Length}, but input length is {val.Length}");
                
            var maxL = p[i].GetCustomAttribute<System.ComponentModel.DataAnnotations.MaxLengthAttribute>();
            if(maxL != null && val.Length > maxL.Length)
                return new HttpResponse(400, "Bad Request", new Dictionary<string, string>(), $"Parameter {p[i].Name.ToLower()} has maximal length {maxL.Length}, but input length is {val.Length}");

#if NET8_0_OR_GREATER
            var L = p[i].GetCustomAttribute<System.ComponentModel.DataAnnotations.LengthAttribute>();
            if(L != null && (val.Length > L.MaximumLength||val.Length < L.MinimumLength))
                return new HttpResponse(400, "Bad Request", new Dictionary<string, string>(), $"Parameter {p[i].Name.ToLower()} has maximal length {L.MaximumLength} and minimal {L.MinimumLength}, but input length is {val.Length}");
#endif
            
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
            return JsonConvert.DeserializeObject(value, type);
        }
        catch
        {
            return null;
        }
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
    
    public MethodInvoke(List<CustomDeserialize> customDeserialization)
    {
        CustomDeserialization = customDeserialization;
    }

    public MethodInvoke()
    {
    }
}