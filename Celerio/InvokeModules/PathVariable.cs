using System.Reflection;
using Newtonsoft.Json;

namespace Celerio.InvokeModules;

public class PathVariable : InputModuleBase
{
    public override bool GetArgumentProvider(ParameterInfo parameter, EndpointManager.Endpoint ep, out InputProvider.ArgumentProvider? provider)
    {
        provider = null;
        if (!ep.Route.DynamicParameters.Contains(parameter.Name!))
            return false;
        
        int parameterIndex = Array.IndexOf(ep.Route.DynamicParameters, parameter.Name!);
        
        provider = (Context context, out object? value, out string? reason) =>
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
        return true;
    }
}