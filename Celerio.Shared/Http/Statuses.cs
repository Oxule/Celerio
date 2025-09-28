namespace Celerio;

/// <summary>
/// Provides HTTP status descriptions indexed by HTTP status code.
/// The array is indexed by numeric status code (e.g., index 200 for "OK").
/// Codes without standard names are marked as "Unknown".
/// </summary>
public static class Statuses
{
    /// <summary>
    /// Array of HTTP status descriptions. Indexed by status code.
    /// For example, Status[200] returns "OK", Status[404] returns "Not Found".
    /// </summary>
    public static string?[] Status =
    [
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Continue", "Switching Protocols", "Processing", "Early Hints", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "OK", "Created", "Accepted",
        "Non-Authoritative Information", "No Content", "Reset Content", "Partial Content", "Multi-Status",
        "Already Reported", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "IM Used",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Multiple Choices", "Moved Permanently", "Found", "See Other", "Not Modified",
        "Use Proxy", "Unknown", "Temporary Redirect", "Permanent Redirect", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Bad Request", "Unauthorized",
        "Payment Required", "Forbidden", "Not Found", "Method Not Allowed", "Not Acceptable",
        "Proxy Authentication Required", "Request Timeout", "Conflict", "Gone", "Length Required",
        "Precondition Failed", "Content Too Large", "URI Too Long", "Unsupported Media Type", "Range Not Satisfiable",
        "Expectation Failed", "Unknown", "Unknown", "Unknown", "Misdirected Request", "Unprocessable Content", "Locked",
        "Failed Dependency", "Too Early", "Upgrade Required", "Unknown", "Precondition Required", "Too Many Requests",
        "Unknown", "Request Header Fields Too Large", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unavailable For Legal Reasons", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown",
        "Unknown", "Unknown", "Unknown", "Unknown", "Internal Server Error", "Not Implemented", "Bad Gateway",
        "Service Unavailable", "Gateway Timeout", "HTTP Version Not Supported", "Variant Also Negotiates",
        "Insufficient Storage", "Loop Detected", "Unknown", "Unknown", "Network Authentication Required"
    ];
}