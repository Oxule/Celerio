using System.Reflection;
using Newtonsoft.Json;

namespace Celerio.InvokeModules;

public class PathVariable : ArgumentType
{
    public override bool NeedsValidation() => true;

    public override bool IsRepresents(ParameterInfo parameter, Endpoint endpoint)
    {
        if (!endpoint.Route.DynamicParameters.Contains(parameter.Name!))
            return false;
        return true;
    }

    public override ArgumentResolver CreateResolver(ParameterInfo parameter, Endpoint ep)
    {
        int parameterIndex = Array.IndexOf(ep.Route.DynamicParameters, parameter.Name!);
        
        return (Context context, out object? value, out string? reason) =>
        {
            reason = null;
            value = null;
            var v = context.PathParameters![parameterIndex];
            if (parameter.ParameterType == typeof(string))
            {
                value = v;
                return true;
            }
            try
            {
                value = JsonConvert.DeserializeObject(v, parameter.ParameterType);
                return true;
            }
            catch (Exception e)
            {
                reason = $"Wrong path parameter [{parameter.Name!}]";
                return false;
            }
        };
    }
}