using System.Reflection;
using System.Text;
using Celerio.InvokeModules;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Celerio;

public class InputProvider
{
    public delegate bool ArgumentProvider(Context context, out object? value, out string? reason);
    
    public readonly ArgumentProvider[] ArgumentProviders;

    public InputProvider(ArgumentProvider[] providers)
    {
        ArgumentProviders = providers;
    }

    public bool ResolveArguments(Context context, out object?[] args, out string? reason)
    {
        reason = null;
        args = new object?[ArgumentProviders.Length];
        for (int i = 0; i < ArgumentProviders.Length; i++)
        {
            if (!ArgumentProviders[i].Invoke(context, out args[i], out reason))
                return false;
        }
        return true;
    }
}

public class InputModuleBase
{
    public virtual bool GetArgumentProvider(ParameterInfo parameter, EndpointManager.Endpoint ep, out InputProvider.ArgumentProvider? provider)
    {
        provider = null;
        return false;
    }
}

public class EndpointInvoker
{
    private List<InputModuleBase> _inputModules = new()
    {
        
    };

    private List<InputModuleBase> _lateInputModules = new()
    {
        new BodyVariable(),
        new ContextVariable(),
        new PathVariable(),
        new QueryVariable()
    };

    internal void ResolveProviders(List<EndpointManager.Endpoint> endpoints)
    {
        Logging.Log("Resolving argument providers...");
        
        foreach (var ep in endpoints)
        {
            var parameters = ep.Method.GetParameters();
            InputProvider.ArgumentProvider[] providers = new InputProvider.ArgumentProvider[parameters.Length];

            var sb = new StringBuilder();
            sb.Append($"Resolved {ep.HttpMethod} {ep.Route.Route}");
            
            for (int i = 0; i < providers.Length; i++)
            {
                providers[i] = FindArgumentProvider(parameters[i], ep, out var provider);
                sb.AppendLine();
                sb.Append($" -{parameters[i].Name}({parameters[i].ParameterType}) => {provider}");
            }

            ep.InputProvider = new InputProvider(providers);
            
            Logging.Log(sb.ToString());
        }
    }
    private InputProvider.ArgumentProvider FindArgumentProvider(ParameterInfo parameter, EndpointManager.Endpoint ep, out string provider)
    {
        provider = "";
        foreach (var module in _inputModules)
            if (module.GetArgumentProvider(parameter, ep, out var p))
            {
                provider = module.GetType().Name;
                return p!;
            }

        foreach (var module in _lateInputModules)
            if (module.GetArgumentProvider(parameter, ep, out var p))
            {
                provider = module.GetType().Name;
                return p!;
            }

        throw new InvalidOperationException($"No argument provider found for parameter {parameter.Name} in endpoint {ep.Method.Name}");
    }

    
    
    internal HttpResponse CallEndpoint(Context context)
    {
        if (!context.Endpoint!.InputProvider.ResolveArguments(context, out var args, out var reason))
            return HttpResponse.BadRequest(reason);
        
        object? respRaw;
        try
        {
            respRaw = context.Endpoint.Method.Invoke(context.Endpoint!.Target, args);
        }
        catch (Exception e)
        {
            return HttpResponse.InternalServerError(e.ToString());
        }
        
        HttpResponse resp;
        
        if (respRaw != null && respRaw.GetType() == typeof(HttpResponse))
            resp = (HttpResponse)respRaw;
        else if (respRaw != null && respRaw is string r)
            resp = HttpResponse.Ok(r);
        else
            resp = HttpResponse.Ok(JsonConvert.SerializeObject(respRaw, new JsonSerializerSettings 
            { 
                ContractResolver = new CamelCasePropertyNamesContractResolver() 
            }));
        return resp;
    }
}