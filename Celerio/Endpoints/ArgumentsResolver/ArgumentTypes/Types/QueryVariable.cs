using System.Reflection;
using System.Text;
using SpanJson;
using SpanJson.Resolvers;

namespace Celerio.InvokeModules;

public class QueryVariable : ArgumentType
{
    public override bool NeedsValidation() => true;

    public override bool IsRepresents(ParameterInfo parameter, Endpoint endpoint)
    {
        return true;
    }
    
    public override ArgumentResolver CreateResolver(ParameterInfo parameter, Endpoint ep)
    {
        if (parameter.ParameterType == typeof(string))
            return (Context context, out object? value, out string? reason) =>
            {
                reason = null;
                value = null;
                if (context.Request.Query.TryGetValue(parameter.Name!, out var q))
                {
                    value = q;
                    return true;
                }
                if (parameter.HasDefaultValue)
                {
                    value = parameter.DefaultValue;
                    return true;
                }
                reason = $"Query parameter [{parameter.Name!}] didn't specified";
                return false;
            };
        
        return (Context context, out object? value, out string? reason) =>
            {
                reason = null;
                value = null;
                if (context.Request.Query.TryGetValue(parameter.Name!, out var q))
                {
                    try
                    {
                        value =  JsonSerializer.NonGeneric.Utf8.Deserialize<ExcludeNullsCamelCaseResolver<byte>>(Encoding.UTF8.GetBytes(q), parameter.ParameterType);
                        return true;
                    }
                    catch (Exception e)
                    {
                        reason = $"Wrong query parameter [{parameter.Name!}]";
                        return false;
                    }
                }
                if (parameter.HasDefaultValue)
                {
                    value = parameter.DefaultValue;
                    return true;
                }
                reason = $"Query parameter [{parameter.Name!}] didn't specified";
                return false;
            };
    }
}