namespace Celerio;

/// <summary>
/// Attribute indicating that a method handles HTTP GET requests.
/// The method will be invoked when a GET request matches the specified pattern.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class GetAttribute : Attribute
{
    /// <summary>
    /// Gets the URL pattern this attribute is applied to.
    /// </summary>
    public string Pattern { get; }

    /// <summary>
    /// Initializes a new instance of GetAttribute with the specified URL pattern.
    /// </summary>
    /// <param name="pattern">The URL pattern to match for GET requests.</param>
    public GetAttribute(string pattern)
    {
        Pattern = pattern;
    }
}

/// <summary>
/// Attribute indicating that a method handles HTTP POST requests.
/// The method will be invoked when a POST request matches the specified pattern.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class PostAttribute : Attribute
{
    /// <summary>
    /// Gets the URL pattern this attribute is applied to.
    /// </summary>
    public string Pattern { get; }

    /// <summary>
    /// Initializes a new instance of PostAttribute with the specified URL pattern.
    /// </summary>
    /// <param name="pattern">The URL pattern to match for POST requests.</param>
    public PostAttribute(string pattern)
    {
        Pattern = pattern;
    }
}

/// <summary>
/// Attribute indicating that a method handles HTTP PUT requests.
/// The method will be invoked when a PUT request matches the specified pattern.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class PutAttribute : Attribute
{
    /// <summary>
    /// Gets the URL pattern this attribute is applied to.
    /// </summary>
    public string Pattern { get; }

    /// <summary>
    /// Initializes a new instance of PutAttribute with the specified URL pattern.
    /// </summary>
    /// <param name="pattern">The URL pattern to match for PUT requests.</param>
    public PutAttribute(string pattern)
    {
        Pattern = pattern;
    }
}

/// <summary>
/// Attribute indicating that a method handles HTTP PATCH requests.
/// The method will be invoked when a PATCH request matches the specified pattern.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class PatchAttribute : Attribute
{
    /// <summary>
    /// Gets the URL pattern this attribute is applied to.
    /// </summary>
    public string Pattern { get; }

    /// <summary>
    /// Initializes a new instance of PatchAttribute with the specified URL pattern.
    /// </summary>
    /// <param name="pattern">The URL pattern to match for PATCH requests.</param>
    public PatchAttribute(string pattern)
    {
        Pattern = pattern;
    }
}

/// <summary>
/// Attribute indicating that a method handles HTTP DELETE requests.
/// The method will be invoked when a DELETE request matches the specified pattern.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class DeleteAttribute : Attribute
{
    /// <summary>
    /// Gets the URL pattern this attribute is applied to.
    /// </summary>
    public string Pattern { get; }

    /// <summary>
    /// Initializes a new instance of DeleteAttribute with the specified URL pattern.
    /// </summary>
    /// <param name="pattern">The URL pattern to match for DELETE requests.</param>
    public DeleteAttribute(string pattern)
    {
        Pattern = pattern;
    }
}
