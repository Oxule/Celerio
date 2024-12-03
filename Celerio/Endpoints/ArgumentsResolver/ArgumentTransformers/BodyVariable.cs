using System.Reflection;
using Newtonsoft.Json;

namespace Celerio.InvokeModules;

public class BodyVariable : ArgumentType
{
    public override bool NeedsValidation() => true;

    public override bool IsRepresents(ParameterInfo parameter, Endpoint endpoint)
    {
        if (parameter.Name != "body")
            return false;
        return true;
    }

    public override ArgumentResolver CreateResolver(ParameterInfo parameter, Endpoint ep)
    {
        if (parameter.ParameterType == typeof(byte[]))
            return (Context context, out object? value, out string? reason) =>
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
        
        if (parameter.ParameterType == typeof(string))
            return (Context context, out object? value, out string? reason) =>
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
        
        if (parameter.ParameterType == typeof(MultipartData))
            return (Context context, out object? value, out string? reason) =>
            {
                value = null;
                if(!MultipartData.TryParse(context.Request, out var data, out reason))
                    return false;
                
                reason = null;
                value = data;
                return true;
            };
        
        return (Context context, out object? value, out string? reason) =>
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
    }
}