using System.Reflection;

namespace Celerio.InvokeModules;

public class ContextVariable : ArgumentType
{
    public override bool IsRepresents(ParameterInfo parameter, Endpoint endpoint)
    {
        if (parameter.ParameterType != typeof(Context))
            return false;
        return true;
    }

    public override ArgumentResolver CreateResolver(ParameterInfo parameter, Endpoint ep)
    {
        return (Context context, out object? value, out string? reason) =>
        {
            reason = null;
            value = context;
            return true;
        };
    }
}