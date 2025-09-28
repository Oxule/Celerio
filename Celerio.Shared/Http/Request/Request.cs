using System.Net;

namespace Celerio;

/// <summary>
/// Represents a parsed HTTP request, containing the method, path, query parameters, headers, body, and client endpoint.
/// This class encapsulates all data parsed from an incoming HTTP request stream.
/// </summary>
public class Request
{
    /// <summary>
    /// The HTTP method/verb of the request (e.g., GET, POST, PUT, DELETE).
    /// </summary>
    public string Method;

    /// <summary>
    /// The percent-decoded path component of the request URI.
    /// </summary>
    public string Path;

    /// <summary>
    /// Dictionary containing decoded query parameters from the request URI.
    /// Keys and values are URL-decoded.
    /// </summary>
    public Dictionary<string, string> Query;

    /// <summary>
    /// Collection of HTTP headers from the request, supporting multiple values per header name.
    /// </summary>
    public HeaderCollection Headers;

    /// <summary>
    /// The raw byte contents of the request body, if any.
    /// May be empty for requests without a body.
    /// </summary>
    public byte[] Body;

    /// <summary>
    /// The remote endpoint representing the client that made this request.
    /// This is typically set by the server handling the connection.
    /// </summary>
    public EndPoint Remote;

    /// <summary>
    /// Initializes a new Request instance with the provided HTTP request components.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">The decoded path of the request URI.</param>
    /// <param name="query">Dictionary of query parameters.</param>
    /// <param name="headers">The header collection.</param>
    /// <param name="body">The request body bytes.</param>
    public Request(string method, string path, Dictionary<string, string> query, HeaderCollection headers, byte[] body)
    {
        Method = method;
        Path = path;
        Query = query;
        Headers = headers;
        Body = body;
    }
}
