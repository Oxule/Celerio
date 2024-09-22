using System.Reflection;
using Newtonsoft.Json;

namespace Celerio.InvokeModules;

public class ContextVariable : InputModuleBase
{
    public override bool GetArgumentProvider(ParameterInfo parameter, EndpointManager.Endpoint ep, out InputProvider.ArgumentProvider? provider)
    {
        provider = null;
        if (parameter.ParameterType != typeof(Context))
            return false;
        
        provider = (Context context, out object? value, out string? reason) =>
        {
            reason = null;
            value = context;
            return true;
        };
        return true;
    }
}