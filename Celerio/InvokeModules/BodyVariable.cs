using System.Reflection;
using Newtonsoft.Json;

namespace Celerio.InvokeModules;

public class BodyVariable : InputModuleBase
{
    public override bool GetArgumentProvider(ParameterInfo parameter, EndpointManager.Endpoint ep, out InputProvider.ArgumentProvider? provider)
    {
        provider = null;

        if (parameter.Name != "body")
            return false;
        
        if (parameter.ParameterType == typeof(byte[]))
            provider = (Context context, out object? value, out string? reason) =>
            {
                if (context.Request.BodyRaw == null || context.Request.BodyRaw.Length == 0)
                {
                    reason = "No request body";
                    value = null;
                    return false;
                }
                reason = null;
                value = context.Request.BodyRaw;
                return true;
            };
        
        else if (parameter.ParameterType == typeof(string))
            provider = (Context context, out object? value, out string? reason) =>
            {
                if (string.IsNullOrEmpty(context.Request.Body))
                {
                    reason = "No request body";
                    value = null;
                    return false;
                }
                reason = null;
                value = context.Request.Body;
                return true;
            };
        
        else if (parameter.ParameterType == typeof(MultipartData))
            provider = (Context context, out object? value, out string? reason) =>
            {
                value = null;
                if(!MultipartData.TryParse(context.Request, out var data, out reason))
                    return false;
                
                reason = null;
                value = data;
                return true;
            };
        
        else
            provider = (Context context, out object? value, out string? reason) =>
            {
                value = null;
                reason = null;
                if (string.IsNullOrEmpty(context.Request.Body))
                {
                    reason = "No request body";
                    return false;
                }
                try
                {
                    value = JsonConvert.DeserializeObject(context.Request.Body, parameter.ParameterType);
                    return true;
                }
                catch (Exception e)
                {
                    reason = "Wrong request body";
                    return false;
                }
            };
        return true;
    }
}