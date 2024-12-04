using System.Reflection;

namespace Celerio;

public class ArgumentType
{
    public delegate bool ArgumentResolver(Context context, out object? value, out string? reason);
    
    public virtual bool NeedsValidation() => false;
    
    public virtual bool IsRepresents(ParameterInfo parameter, Endpoint endpoint)
    {
        return false;
    }

    public virtual ArgumentResolver? CreateResolver(ParameterInfo parameter, Endpoint ep)
    {
        return null;
    }
}