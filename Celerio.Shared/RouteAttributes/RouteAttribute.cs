namespace Celerio;

/// <summary>
/// Attribute for defining custom HTTP routes that do not fit standard methods.
/// Allows specifying arbitrary HTTP methods and URL patterns for method routing.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class RouteAttribute : Attribute
{
    /// <summary>
    /// Gets the HTTP method name for this route (e.g., "GET", "POST", "CUSTOM").
    /// </summary>
    public string Method { get; }

    /// <summary>
    /// Gets the URL pattern this attribute is applied to.
    /// </summary>
    public string Pattern { get; }

    /// <summary>
    /// Initializes a new instance of RouteAttribute with the specified HTTP method and URL pattern.
    /// </summary>
    /// <param name="method">The HTTP method name.</param>
    /// <param name="pattern">The URL pattern to match.</param>
    public RouteAttribute(string method, string pattern)
    {
        Method = method;
        Pattern = pattern;
    }
}
