using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Celerio;

public static class ResponseResolver
{
    public static HttpResponse Resolve(object? respRaw)
    {
        if (respRaw != null && respRaw.GetType() == typeof(HttpResponse))
            return (HttpResponse)respRaw;
        if (respRaw != null && respRaw is string r)
            return HttpResponse.Ok(r);
            
        return HttpResponse.Ok(JsonConvert.SerializeObject(respRaw, new JsonSerializerSettings 
            { 
                ContractResolver = new CamelCasePropertyNamesContractResolver() 
            }));
    }
}