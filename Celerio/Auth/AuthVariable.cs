using System.Reflection;

namespace Celerio.InvokeModules;

public class AuthVariable : ArgumentType
{
    public override bool IsRepresents(ParameterInfo parameter, Endpoint endpoint)
    {
        if (parameter.Name != "auth")
            return false;
        return true;
    }

    public override ArgumentResolver CreateResolver(ParameterInfo parameter, Endpoint ep)
    {
        return (Context context, out object? value, out string? reason) =>
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
    }
}