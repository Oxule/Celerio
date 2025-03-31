using System.Text;
using SpanJson;

namespace Celerio;

public class HttpResponse
{
    public int StatusCode { get; set; }
    public string StatusMessage { get; set; }
    public HeadersCollection Headers { get; set; } = new ();
    public byte[]? Body { get; set; }
    
    public void PreProcess()
    {
        SetHeader("Server", "Celerio/1.1");
        SetHeader("Date", DateTime.UtcNow.ToString("r"));
        SetHeader("Connection", "keep-alive");

        if (Body != null && Body.Length != 0)
        {
            SetHeader("Content-Length", Body.Length.ToString());
            if (!Headers.Contains("Content-Type"))
                SetHeader("Content-Type", "text/plain; charset=utf-8");
        }
        else
        {
            SetHeader("Content-Length", 0.ToString());
        }
    }

    public HttpResponse SetBody(byte[] body)
    {
        Body = body;
        return this;
    }
    public HttpResponse SetBody(string body)
    {
        Body = Encoding.UTF8.GetBytes(body);
        return this;
    }
    public HttpResponse SetHeader(string name, string value)
    {
        Headers.Set(name, value);
        return this;
    }
    
    public HttpResponse AddHeader(string name, string value)
    {
        Headers.Add(name, value);
        return this;
    }

    public HttpResponse SetStatus(int code, string message = "Message Didn't Specified")
    {
        StatusCode = code;
        StatusMessage = message;
        return this;
    }
    
    public HttpResponse(int statusCode, string statusMessage, string? body = null)
    {
        StatusCode = statusCode;
        StatusMessage = statusMessage;
        if(body != null)
            SetBody(body);
    }
    public HttpResponse(int statusCode, string statusMessage, byte[]? body = null)
    {
        StatusCode = statusCode;
        StatusMessage = statusMessage;
        if(body != null)
            SetBody(body);
    }
    
    public HttpResponse(int statusCode, string statusMessage)
    {
        StatusCode = statusCode;
        StatusMessage = statusMessage;
    }

    public static HttpResponse File(string path)
    {
        return File(new FileStream(path, FileMode.Open, FileAccess.Read), MIME.GetType(Path.GetExtension(path)));
    }
    public static HttpResponse File(Stream stream, string contentType)
    {
        var buffer = new byte[stream.Length];
        if (stream.Read(buffer, 0, (int)stream.Length) == stream.Length)
        {
            return new HttpResponse(200, "OK")
                .SetBody(buffer)
                .SetHeader("Content-Type", contentType);
        }

        return InternalServerError("Can't read file");
    }
    
    public static HttpResponse Ok(string body = "") => new (200, "OK", body);
    public static HttpResponse Ok(byte[] body) => new (200, "OK", body);
    public static HttpResponse Created(string body = "") => new (201, "Created", body);

    
    public static HttpResponse BadRequest(string reason, int code = 0) => new (400, "Bad Request", $"{{\"reason\":\"${reason}\",\"code\":${code}}}");
    public static HttpResponse PermanentRedirect(string location) => new HttpResponse(308, "Permanent Redirect").SetHeader("Location", location);
    public static HttpResponse TemporaryRedirect(string location) => new HttpResponse(307, "Temporary Redirect").SetHeader("Location", location);
    
    public static HttpResponse Unauthorized() => new (401, "Unauthorized");
    
    public static HttpResponse Forbidden() => new (403, "Forbidden");

    public static HttpResponse NotFound() => new (404, "Not Found");

    public static HttpResponse InternalServerError(string message) => new (500, "Internal Server Error", message);

    public static HttpResponse NotImplemented() => new (501, "Not Implemented");

    public static HttpResponse ServiceUnavailable() => new (503, "Service Unavailable");
    
    public static HttpResponse ImATeapot() => new (418, "I'm a teapot");
}