using System.Net.Sockets;
using System.Text;

namespace Celerio;

/// <summary>
/// Represents an HTTP response, encapsulating status code, headers, and body.
/// Provides fluent methods for building responses and factory methods for common HTTP statuses.
/// </summary>
public class Result
{
    /// <summary>
    /// The HTTP status code of the response (e.g., 200, 404).
    /// </summary>
    public int StatusCode;

    /// <summary>
    /// The body content of the response.
    /// Defaults to an empty body if not specified.
    /// </summary>
    public BaseResultBody Body = new ();

    /// <summary>
    /// The collection of HTTP headers to include in the response.
    /// </summary>
    public HeaderCollection Headers = new ();

    /// <summary>
    /// Asynchronously writes the complete HTTP response to the provided network stream.
    /// Includes status line (with status reason), default headers (Date, Server), custom headers,
    /// body headers (Content-Type, etc.), and the response body.
    /// </summary>
    /// <param name="stream">The network stream to write the response to.</param>
    /// <returns>A Task representing the asynchronous write operation.</returns>
    public async Task WriteResultAsync(NetworkStream stream)
    {
        var writer = new StreamWriter(stream, Encoding.ASCII);
        /*
        await writer.WriteLineAsync($"HTTP/1.1 {StatusCode} {Statuses.Status[StatusCode]}");
        */
        await writer.WriteLineAsync($"HTTP/1.1 {StatusCode} OPT\nDate: {DateTime.UtcNow.ToString("r")}\nServer: Celerio/2.0");
        await Headers.WriteHeadersAsync(writer);
        await Body.WriteBodyHeadersAsync(writer);
        await writer.WriteLineAsync();

        await writer.FlushAsync();
        await Body.WriteBodyAsync(stream);
    }

    /// <summary>
    /// Initializes a Result with the specified HTTP status code.
    /// Body and headers are initialized to defaults (empty body, empty headers).
    /// </summary>
    /// <param name="statusCode">The HTTP status code for the response.</param>
    public Result(int statusCode)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Initializes a Result with the specified HTTP status code and response body.
    /// Headers are initialized to empty.
    /// </summary>
    /// <param name="statusCode">The HTTP status code for the response.</param>
    /// <param name="body">The body content for the response.</param>
    public Result(int statusCode, BaseResultBody body)
    {
        StatusCode = statusCode;
        Body = body;
    }

    /// <summary>
    /// Initializes a Result with the specified HTTP status code, response body, and headers.
    /// </summary>
    /// <param name="statusCode">The HTTP status code for the response.</param>
    /// <param name="body">The body content for the response.</param>
    /// <param name="headers">The collection of HTTP headers.</param>
    public Result(int statusCode, BaseResultBody body, HeaderCollection headers)
    {
        StatusCode = statusCode;
        Body = body;
        Headers = headers;
    }

    /// <summary>
    /// Adds a header to the response. Supports chaining multiple headers.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <param name="value">The header value.</param>
    /// <returns>The current Result instance for fluent chaining.</returns>
    public Result Header(string name, string value)
    {
        Headers.Add(name, value);
        return this;
    }

    /// <summary>
    /// Sets the value for a header, overriding any existing values.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <param name="value">The header value.</param>
    /// <returns>The current Result instance for fluent chaining.</returns>
    public Result SetHeader(string name, string value)
    {
        Headers.Set(name, value);
        return this;
    }

    /// <summary>
    /// Sets the body content for the response.
    /// </summary>
    /// <param name="body">The body content to set.</param>
    /// <returns>The current Result instance for fluent chaining.</returns>
    public Result SetBody(BaseResultBody body)
    {
        Body = body;
        return this;
    }


    #region Statuses

    // 2xx Success
    /// <summary>
    /// Creates a Result with HTTP status code 200 (OK), indicating successful request processing.
    /// </summary>
    /// <returns>A Result with status 200.</returns>
    public static Result Ok() => new(200);

    /// <summary>
    /// Creates a Result with HTTP status code 201 (Created), indicating successful resource creation.
    /// Includes a Location header pointing to the created resource.
    /// </summary>
    /// <param name="location">The URI of the created resource.</param>
    /// <returns>A Result with status 201 and Location header.</returns>
    public static Result Created(string location) =>
        new Result(201).Header("Location", location);

    /// <summary>
    /// Creates a Result with HTTP status code 204 (No Content), indicating successful processing with no response body.
    /// </summary>
    /// <returns>A Result with status 204.</returns>
    public static Result NoContent() => new(204);

    /// <summary>
    /// Creates a Result with HTTP status code 202 (Accepted), indicating request accepted but not yet processed.
    /// </summary>
    /// <returns>A Result with status 202.</returns>
    public static Result Accepted() => new(202);

    // 3xx Redirection
    /// <summary>
    /// Creates a Result with HTTP status code 301 (Moved Permanently), indicating resource moved to a new location.
    /// Includes a Location header with the new URI.
    /// </summary>
    /// <param name="location">The new URI of the resource.</param>
    /// <returns>A Result with status 301 and Location header.</returns>
    public static Result MovedPermanently(string location) =>
        new Result(301).Header("Location", location);

    /// <summary>
    /// Creates a Result with HTTP status code 302 (Found), indicating resource temporarily moved to another location.
    /// Includes a Location header with the temporary URI.
    /// </summary>
    /// <param name="location">The temporary URI of the resource.</param>
    /// <returns>A Result with status 302 and Location header.</returns>
    public static Result Found(string location) =>
        new Result(302).Header("Location", location);

    /// <summary>
    /// Creates a Result with HTTP status code 303 (See Other), directing to a different resource for the response.
    /// Typically used for POST responses to redirect to a GET page.
    /// </summary>
    /// <param name="location">The URI to redirect to for the response.</param>
    /// <returns>A Result with status 303 and Location header.</returns>
    public static Result SeeOther(string location) =>
        new Result(303).Header("Location", location);

    /// <summary>
    /// Creates a Result with HTTP status code 307 (Temporary Redirect), indicating temporary redirection without changing method.
    /// The client should use the same HTTP method for the new location.
    /// </summary>
    /// <param name="location">The temporary URI of the resource.</param>
    /// <returns>A Result with status 307 and Location header.</returns>
    public static Result TemporaryRedirect(string location) =>
        new Result(307).Header("Location", location);

    /// <summary>
    /// Creates a Result with HTTP status code 308 (Permanent Redirect), indicating permanent redirection without changing method.
    /// The client should use the same HTTP method for the new location.
    /// </summary>
    /// <param name="location">The permanent URI of the resource.</param>
    /// <returns>A Result with status 308 and Location header.</returns>
    public static Result PermanentRedirect(string location) =>
        new Result(308).Header("Location", location);

    // 4xx Client Errors
    /// <summary>
    /// Creates a Result with HTTP status code 400 (Bad Request), indicating malformed or invalid request syntax.
    /// </summary>
    /// <returns>A Result with status 400.</returns>
    public static Result BadRequest() => new(400);

    /// <summary>
    /// Creates a Result with HTTP status code 401 (Unauthorized), indicating authentication is required or failed.
    /// </summary>
    /// <returns>A Result with status 401.</returns>
    public static Result Unauthorized() => new(401);

    /// <summary>
    /// Creates a Result with HTTP status code 403 (Forbidden), indicating access denial to the requested resource.
    /// </summary>
    /// <returns>A Result with status 403.</returns>
    public static Result Forbidden() => new(403);

    /// <summary>
    /// Creates a Result with HTTP status code 404 (Not Found), indicating the requested resource was not found.
    /// </summary>
    /// <returns>A Result with status 404.</returns>
    public static Result NotFound() => new(404);

    /// <summary>
    /// Creates a Result with HTTP status code 409 (Conflict), indicating request conflicts with current state.
    /// </summary>
    /// <returns>A Result with status 409.</returns>
    public static Result Conflict() => new(409);

    /// <summary>
    /// Creates a Result with HTTP status code 410 (Gone), indicating the resource is no longer available.
    /// </summary>
    /// <returns>A Result with status 410.</returns>
    public static Result Gone() => new(410);

    /// <summary>
    /// Creates a Result with HTTP status code 415 (Unsupported Media Type), indicating unsupported content type.
    /// </summary>
    /// <returns>A Result with status 415.</returns>
    public static Result UnsupportedMediaType() => new(415);

    /// <summary>
    /// Creates a Result with HTTP status code 422 (Unprocessable Entity), indicating semantic errors in request.
    /// </summary>
    /// <returns>A Result with status 422.</returns>
    public static Result UnprocessableEntity() => new(422);

    // 5xx Server Errors
    /// <summary>
    /// Creates a Result with HTTP status code 500 (Internal Server Error), indicating an unexpected server error.
    /// </summary>
    /// <returns>A Result with status 500.</returns>
    public static Result InternalServerError() => new(500);

    /// <summary>
    /// Creates a Result with HTTP status code 501 (Not Implemented), indicating requested functionality is not supported.
    /// </summary>
    /// <returns>A Result with status 501.</returns>
    public static Result NotImplemented() => new(501);

    /// <summary>
    /// Creates a Result with HTTP status code 502 (Bad Gateway), indicating invalid response from upstream server.
    /// </summary>
    /// <returns>A Result with status 502.</returns>
    public static Result BadGateway() => new(502);

    /// <summary>
    /// Creates a Result with HTTP status code 503 (Service Unavailable), indicating server temporarily unable to handle requests.
    /// </summary>
    /// <returns>A Result with status 503.</returns>
    public static Result ServiceUnavailable() => new(503);

    /// <summary>
    /// Creates a Result with HTTP status code 504 (Gateway Timeout), indicating timeout from upstream server.
    /// </summary>
    /// <returns>A Result with status 504.</returns>
    public static Result GatewayTimeout() => new(504);
    
    #endregion
}
