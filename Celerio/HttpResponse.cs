namespace Celerio;

public class HttpResponse
{
    public int StatusCode { get; set; }
    public string StatusMessage { get; set; }
    public Dictionary<string, string> Headers { get; set; }
    public byte[]? BodyRaw { get; set; }
    public string? Body { get; set; }

    public HttpResponse(int statusCode, string statusMessage, Dictionary<string, string> headers, string body)
    {
        StatusCode = statusCode;
        StatusMessage = statusMessage;
        Headers = headers;
        Body = body;
    }
    public HttpResponse(int statusCode, string statusMessage, Dictionary<string, string> headers,byte[] body)
    {
        StatusCode = statusCode;
        StatusMessage = statusMessage;
        Headers = headers;
        BodyRaw = body;
    }

    public static HttpResponse File(string path, string contentType)
    {
        return File(new FileStream(path, FileMode.Open, FileAccess.Read), contentType);
    }
    public static HttpResponse File(Stream stream, string contentType)
    {
        var buffer = new byte[stream.Length];
        stream.Read(buffer, 0, (int)stream.Length);
        return new HttpResponse(200, "OK", new Dictionary<string, string>() {{"Content-Type", contentType}}, buffer);
    }
    
    public static HttpResponse Ok(string body) => new HttpResponse(200, "OK", new Dictionary<string, string>(), body);
    
    public static HttpResponse Created(string location) => new HttpResponse(201, "Created", new Dictionary<string, string> { { "Location", location } }, "");

    public static HttpResponse BadRequest(string message) => new HttpResponse(400, "Bad Request", new Dictionary<string, string>(), message);

    public static HttpResponse Unauthorized() => new HttpResponse(401, "Unauthorized", new Dictionary<string, string>(), "");

    public static HttpResponse Forbidden() => new HttpResponse(403, "Forbidden", new Dictionary<string, string>(), "");

    public static HttpResponse NotFound() => new HttpResponse(404, "Not Found", new Dictionary<string, string>(), "");

    public static HttpResponse MethodNotAllowed() => new HttpResponse(405, "Method Not Allowed", new Dictionary<string, string>(), "");

    public static HttpResponse InternalServerError(string message) => new HttpResponse(500, "Internal Server Error", new Dictionary<string, string>(), message);

    public static HttpResponse NotImplemented() => new HttpResponse(501, "Not Implemented", new Dictionary<string, string>(), "");

    public static HttpResponse ServiceUnavailable() => new HttpResponse(503, "Service Unavailable", new Dictionary<string, string>(), "");
    
    public static HttpResponse ImATeapot() => new HttpResponse(418, "I'm a teapot", new Dictionary<string, string>(), "");

    
}