using SpanJson;
using SpanJson.Resolvers;

namespace Celerio;

public static class ResponseResolver
{
    public static HttpResponse Resolve(object? respRaw)
    {
        if (respRaw != null && respRaw.GetType() == typeof(HttpResponse))
            return (HttpResponse)respRaw;
        if (respRaw is string r)
            return HttpResponse.Ok(r);
            
        return HttpResponse.Ok(JsonSerializer.NonGeneric.Utf8.Serialize<ExcludeNullsCamelCaseResolver<byte>>(respRaw));
    }
}