using System.Reflection;
using Newtonsoft.Json;

namespace Celerio.InvokeModules;

public class AuthVariable : InputModuleBase
{
    public override bool GetArgumentProvider(ParameterInfo parameter, EndpointManager.Endpoint ep, out InputProvider.ArgumentProvider? provider)
    {
        provider = null;
        if (parameter.Name != "auth")
            return false;
        
        provider = (Context context, out object? value, out string? reason) =>
        {
            if (context.Identity != null)
            {
                try
                {
                    value = Convert.ChangeType(context.Identity, parameter.ParameterType);
                    reason = null;
                    return true;
                }
                catch { }
            }
            value = null;
            reason = null;
            return true;
        };
        return true;
    }
}