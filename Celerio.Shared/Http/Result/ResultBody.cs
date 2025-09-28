using System.Globalization;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Celerio;

/// <summary>
/// Base class for HTTP response body content.
/// Provides virtual methods for writing body-related headers and the body itself.
/// </summary>
public class BaseResultBody
{
    /// <summary>
    /// Writes the HTTP headers related to the response body to the specified StreamWriter.
    /// Default implementation writes Content-Length: 0 for empty bodies.
    /// </summary>
    /// <param name="writer">The StreamWriter to write body headers to.</param>
    /// <returns>A Task representing the asynchronous write operation.</returns>
    public virtual async Task WriteBodyHeadersAsync(StreamWriter writer)
    {
        await writer.WriteLineAsync("Content-Length: 0");
    }

    /// <summary>
    /// Writes the response body content to the specified NetworkStream.
    /// Default implementation does nothing for empty bodies.
    /// </summary>
    /// <param name="stream">The NetworkStream to write the body content to.</param>
    /// <returns>A Task representing the asynchronous write operation.</returns>
    public virtual async Task WriteBodyAsync(NetworkStream stream) { }
}

/// <summary>
/// Default implementation of BaseResultBody for byte array content.
/// Handles content type and length headers automatically.
/// </summary>
public class DefaultResultBody : BaseResultBody
{
    /// <summary>
    /// The byte content of the response body.
    /// </summary>
    public byte[] Body;

    /// <summary>
    /// The MIME content type of the body.
    /// </summary>
    public string ContentType;

    /// <summary>
    /// Initializes a DefaultResultBody with binary content.
    /// </summary>
    /// <param name="body">The byte array content.</param>
    /// <param name="contentType">The MIME type. Defaults to "application/octet-stream".</param>
    public DefaultResultBody(byte[] body, string contentType = "application/octet-stream")
    {
        Body = body;
        ContentType = contentType;
    }

    /// <summary>
    /// Initializes a DefaultResultBody with string content, encoded as UTF-8.
    /// </summary>
    /// <param name="body">The string content to encode.</param>
    /// <param name="contentType">The MIME type base. Charset utf-8 is appended automatically.</param>
    public DefaultResultBody(string body, string contentType = "text/plain")
    {
        Body = Encoding.UTF8.GetBytes(body);
        ContentType = contentType+";charset=utf-8";
    }

    /// <summary>
    /// Writes Content-Length and Content-Type headers.
    /// </summary>
    /// <param name="writer">The StreamWriter to write the headers to.</param>
    /// <returns>A Task representing the asynchronous write operation.</returns>
    public override async Task WriteBodyHeadersAsync(StreamWriter writer)
    {
        await writer.WriteLineAsync($"Content-Length: {Body.Length}\nContent-Type: {ContentType}");
    }

    /// <summary>
    /// Writes the body bytes to the network stream.
    /// </summary>
    /// <param name="stream">The NetworkStream to write to.</param>
    /// <returns>A Task representing the asynchronous write operation.</returns>
    public override async Task WriteBodyAsync(NetworkStream stream)
    {
        await stream.WriteAsync(Body, 0, Body.Length);
    }
}

/// <summary>
/// Utility methods for creating and setting response body content.
/// Provides factory methods and extension methods for common content types.
/// </summary>
public static class Body
{
    /// <summary>
    /// Creates a text-based response body.
    /// </summary>
    /// <param name="str">The text content.</param>
    /// <returns>A DefaultResultBody with text content.</returns>
    public static DefaultResultBody Text(string str) => new (str);

    /// <summary>
    /// Sets the body of a Result to the specified text content.
    /// </summary>
    /// <param name="result">The Result to modify.</param>
    /// <param name="str">The text content.</param>
    /// <returns>The modified Result with the text body.</returns>
    public static Result Text(this Result result, string str) => result.SetBody(Text(str));

    /// <summary>
    /// Sets the body of a Result to a formatted string representation of the value.
    /// Uses invariant culture for formatting IFormattable types.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The Result to modify.</param>
    /// <param name="value">The value to convert to text.</param>
    /// <returns>The modified Result with the formatted text body.</returns>
    public static Result Text<T>(this Result result, T value)
        where T : IFormattable
    {
        var s = value.ToString(null, CultureInfo.InvariantCulture);
        return result.Text(s);
    }

    /// <summary>
    /// Sets the body of a Result to the string representation of the object.
    /// </summary>
    /// <param name="result">The Result to modify.</param>
    /// <param name="value">The object to convert to text.</param>
    /// <returns>The modified Result with the object's string representation as body.</returns>
    public static Result Text(this Result result, object value)
    {
        var s = value.ToString();
        return result.Text(s);
    }

    /// <summary>
    /// Creates a JSON response body from the specified object.
    /// Uses System.Text.Json for serialization.
    /// </summary>
    /// <param name="obj">The object to serialize to JSON.</param>
    /// <returns>A DefaultResultBody with JSON content.</returns>
    public static DefaultResultBody Json(object obj) => new (JsonSerializer.Serialize(obj), "application/json");

    /// <summary>
    /// Sets the body of a Result to the JSON representation of the object.
    /// </summary>
    /// <param name="result">The Result to modify.</param>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>The modified Result with JSON body.</returns>
    public static Result Json(this Result result, object obj) => result.SetBody(Json(obj));

    /// <summary>
    /// Creates an HTML response body.
    /// </summary>
    /// <param name="html">The HTML content.</param>
    /// <returns>A DefaultResultBody with HTML content.</returns>
    public static DefaultResultBody Html(string html) => new (html, "text/html");

    /// <summary>
    /// Sets the body of a Result to the specified HTML content.
    /// </summary>
    /// <param name="result">The Result to modify.</param>
    /// <param name="html">The HTML content.</param>
    /// <returns>The modified Result with HTML body.</returns>
    public static Result Html(this Result result, string html) => result.SetBody(Html(html));
}
