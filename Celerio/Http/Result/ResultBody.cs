using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Celerio;

public class BaseResultBody
{
    public virtual async Task WriteBodyHeadersAsync(StreamWriter writer)
    {
        await writer.WriteLineAsync("Content-Length: 0");
    }

    public virtual async Task WriteBodyAsync(NetworkStream stream) { }
}

public class DefaultResultBody : BaseResultBody
{
    public byte[] Body;
    public string ContentType;

    public DefaultResultBody(byte[] body, string contentType = "application/octet-stream")
    {
        Body = body;
        ContentType = contentType;
    }

    public DefaultResultBody(string body, string contentType = "text/plain")
    {
        Body = Encoding.UTF8.GetBytes(body);
        ContentType = contentType+";charset=utf-8";
    }

    public override async Task WriteBodyHeadersAsync(StreamWriter writer)
    {
        await writer.WriteLineAsync($"Content-Length: {Body.Length}\nContent-Type: {ContentType}");
    }

    public override async Task WriteBodyAsync(NetworkStream stream)
    {
        await stream.WriteAsync(Body, 0, Body.Length);
    }
}

public static class Body
{
    public static DefaultResultBody Text(string str) => new (str);
    public static Result Text(this Result result, string str) => result.SetBody(Text(str));
    
    
    //TODO: Rewrite to custom serializer
    public static DefaultResultBody Json(object obj) => new (JsonSerializer.Serialize(obj), "application/json");
    public static Result Json(this Result result, object obj) => result.SetBody(Json(obj));
    
    public static DefaultResultBody Html(string html) => new (html, "text/html");
    public static Result Html(this Result result, string html) => result.SetBody(Html(html));
}