using System.Reflection;
using System.Text;
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
                if (context.Request.Body == null || context.Request.Body.Length == 0)
                {
                    reason = "No request body";
                    value = null;
                    return false;
                }
                reason = null;
                value = context.Request.Body;
                return true;
            };
        
        if (parameter.ParameterType == typeof(string))
            return (Context context, out object? value, out string? reason) =>
            {
                if (context.Request.Body == null || context.Request.Body.Length == 0)
                {
                    reason = "No request body";
                    value = null;
                    return false;
                }
                var s = Encoding.UTF8.GetString(context.Request.Body);
                if (string.IsNullOrEmpty(s))
                {
                    value = null;
                    reason = "Request body empty";
                    return false;
                }
                reason = null;
                value = s;
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
                if (context.Request.Body == null || context.Request.Body.Length == 0)
                {
                    reason = "No request body";
                    value = null;
                    return false;
                }
                var s = Encoding.UTF8.GetString(context.Request.Body);
                if (string.IsNullOrEmpty(s))
                {
                    value = null;
                    reason = "Request body empty";
                    return false;
                }
                try
                {
                    reason = "Request body empty";
                    value = JsonConvert.DeserializeObject(s, parameter.ParameterType);
                    return true;
                }
                catch (Exception e)
                {
                    value = null;
                    reason = "Wrong request body";
                    return false;
                }
            };
    }
}