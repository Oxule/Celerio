using System.Reflection;
using Newtonsoft.Json;

namespace Celerio.InvokeModules;

public class QueryVariable : InputModuleBase
{
    public override bool GetArgumentProvider(ParameterInfo parameter, EndpointManager.Endpoint ep, out InputProvider.ArgumentProvider? provider)
    {
        if (parameter.ParameterType == typeof(string))
            provider = (Context context, out object? value, out string? reason) =>
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
        
        else
            provider = (Context context, out object? value, out string? reason) =>
            {
                reason = null;
                value = null;
                if (context.Request.Query.TryGetValue(parameter.Name!, out var q))
                {
                    try
                    {
                        value = JsonConvert.DeserializeObject(q, parameter.ParameterType);
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
        
        return true;
    }
}