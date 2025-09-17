using System.Net.Sockets;
using System.Text;

namespace Celerio;

public class Result
{
    public int StatusCode;

    public BaseResultBody Body = new ();

    public HeaderCollection Headers = new ();

    private static async Task WriteDefaultHeadersAsync(StreamWriter writer)
    {
        await writer.WriteLineAsync($"Date: {DateTime.UtcNow.ToString("r")}");
        await writer.WriteLineAsync("Server: Celerio/2.0");
    }
    
    public async Task WriteResultAsync(NetworkStream stream)
    {
        var writer = new StreamWriter(stream, Encoding.ASCII);
        await writer.WriteLineAsync($"HTTP/1.1 {StatusCode} {Statuses.Status[StatusCode]}");
        await WriteDefaultHeadersAsync(writer);
        await Headers.WriteHeadersAsync(writer);
        await Body.WriteBodyHeadersAsync(writer);
        await writer.WriteLineAsync();
        
        await writer.FlushAsync();
        await Body.WriteBodyAsync(stream);
    }
    
    public Result(int statusCode)
    {
        StatusCode = statusCode;
    }
    
    public Result(int statusCode, BaseResultBody body)
    {
        StatusCode = statusCode;
        Body = body;
    }
    
    public Result(int statusCode, BaseResultBody body, HeaderCollection headers)
    {
        StatusCode = statusCode;
        Body = body;
        Headers = headers;
    }

    public Result Header(string name, string value)
    {
        Headers.Add(name, value);
        return this;
    }
    public Result SetHeader(string name, string value)
    {
        Headers.Set(name, value);
        return this;
    }
    public Result SetBody(BaseResultBody body)
    {
        Body = body;
        return this;
    }


    #region Statuses
    
    // 2xx Success
    public static Result Ok() => new(200);

    public static Result Created(string location) =>
        new Result(201).Header("Location", location);

    public static Result NoContent() => new(204);
    public static Result Accepted() => new(202);

    // 3xx Redirection
    public static Result MovedPermanently(string location) =>
        new Result(301).Header("Location", location);
    public static Result Found(string location) =>
        new Result(302).Header("Location", location);
    public static Result SeeOther(string location) =>
        new Result(303).Header("Location", location);
    public static Result TemporaryRedirect(string location) =>
        new Result(307).Header("Location", location);
    public static Result PermanentRedirect(string location) =>
        new Result(308).Header("Location", location);

    // 4xx Client Errors
    public static Result BadRequest() => new(400);

    public static Result Unauthorized() => new(401);
    public static Result Forbidden() => new(403);

    public static Result NotFound() => new(404);

    public static Result Conflict() => new(409);
    public static Result Gone() => new(410);

    public static Result UnsupportedMediaType() => new(415);
    public static Result UnprocessableEntity() => new(422);

    // 5xx Server Errors
    public static Result InternalServerError() => new(500);
    public static Result NotImplemented() => new(501);
    public static Result BadGateway() => new(502);
    public static Result ServiceUnavailable() => new(503);
    public static Result GatewayTimeout() => new(504);
    
    #endregion
}