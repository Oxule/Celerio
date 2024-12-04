using System.Reflection;
using System.Text;
using Celerio.InvokeModules;

namespace Celerio;

public class Arguments
{
    private static readonly List<ArgumentType> _argumentTypes = new()
    {
        new AuthVariable(),
        new BodyVariable(),
        new ContextVariable(),
        new PathVariable(),
        new QueryVariable()
    };
    
    public class Argument
    {
        public readonly ArgumentType.ArgumentResolver Resolver;
        public readonly ArgumentValidator.Validator? Validator = null;

        public Argument(ArgumentType.ArgumentResolver resolver, ArgumentValidator.Validator? validator = null)
        {
            Resolver = resolver;
            Validator = validator;
        }
    }

    private readonly Argument[] _arguments;

    public static Arguments GetArguments(Endpoint endpoint)
    {
        var parameters = endpoint.Method.GetParameters();
        
        Argument[] arguments = new Argument[parameters.Length];
        
        for (int i = 0; i < arguments.Length; i++)
        {
            var type = FindType(parameters[i], endpoint);
            var resolver = type.CreateResolver(parameters[i], endpoint)!;
            ArgumentValidator.Validator? validator = null;

            if (type.NeedsValidation())
                validator = ArgumentValidator.CreateValidator(parameters[i].ParameterType,
                    parameters[i].GetCustomAttributes(true), parameters[i].Name!);
            
            arguments[i] = new Argument(resolver, validator);
        }

        return new Arguments(arguments);
    }

    public Arguments(Argument[] arguments)
    {
        _arguments = arguments;
    }

    public bool Resolve(Context context, out object?[] args, out string? reason)
    {
        reason = null;
        args = new object?[_arguments.Length];
        for (int i = 0; i < _arguments.Length; i++)
        {
            if (!_arguments[i].Resolver.Invoke(context, out args[i], out reason))
                return false;
        }
        return true;
    }

    public ArgumentValidator.ValidationResult Validate(object?[] args)
    {
        for (int i = 0; i < _arguments.Length; i++)
        {
            if(_arguments[i].Validator == null)
                continue;
            var result = _arguments[i].Validator!.Invoke(args[i]);
            if (!result.Valid)
                return result;
        }

        return new();
    }
    
    private static ArgumentType FindType(ParameterInfo parameter, Endpoint endpoint)
    {
        foreach (var type in _argumentTypes)
            if (type.IsRepresents(parameter, endpoint))
                return type;

        throw new Exception($"Cannot find representing argument type: {parameter.Name}");
    }
}