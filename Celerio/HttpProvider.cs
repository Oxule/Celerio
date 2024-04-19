using System.Text;

namespace Celerio;

public interface IHttpProvider
{
    public string ErrorMessage { get; }
    public bool GetRequest(Stream stream, out HttpRequest request);
    public void SendResponse(Stream stream, HttpResponse response);
}
public class Http11ProtocolProvider : IHttpProvider
{
    public string ErrorMessage { get; } = "HTTP/1.1 400 Protocol Not Supported";
    
    public bool GetRequest(Stream stream, out HttpRequest request)
    {
        string uri = "";
        request = new HttpRequest();
        int pointer = 0;
        while (true)
        {
            var l = ReadLineStream(stream);
            if (l == null)
                break;
            
            if (pointer == 0)
            {
                var p = l.Split(' ');
                if (p.Length != 3)
                    return false;
                if (p[2] != "HTTP/1.1")
                    return false;
                
                request.Method = p[0];
                uri = p[1];
            }
            else
            {
                if (l == "")
                    break;
                var p = l.Split(": ");
                if (request.Headers.ContainsKey(p[0]))
                    return false;
                request.Headers.Add(p[0], string.Join(": ", p.Skip(1)));
            }
            
            pointer += l.Length + 1;
        }
        
        if (request.Headers.TryGetValue("Content-Length", out var contentLength)&&int.TryParse(contentLength, out var length))
        {
            byte[] buffer = new byte[length];
            if (stream.Read(buffer, 0, length) != length)
                return false;
            request.Body = Encoding.UTF8.GetString(buffer);
        }

        var q = uri.Split('?');
        request.URI = q[0];
        if (q.Length == 2)
        {
            foreach (var qq in q[1].Split('&'))
            {
                var p = qq.Split('=');
                if (p.Length != 2)
                    return false;
                if (request.Query.ContainsKey(p[0]))
                    return false;
                
                request.Query.Add(p[0], p[1]);
            }
        }

        if (request.Method == "" || request.URI == "")
            return false;
        
        return true;
    }

    private static string? ReadLineStream(Stream stream)
    {
        List<byte> buffer = new List<byte>();
        while (true)
        {
            var b = stream.ReadByte();
            if (b == -1)
                return null;
            if (b == 10)
            {
                break;
            }
            buffer.Add((byte)b);
        }
        
        return Encoding.UTF8.GetString(buffer.ToArray()).Trim();
    }
    
    private void DefaultHeaders(HttpResponse resp)
    {
        List<KeyValuePair<string, string>> headers = new List<KeyValuePair<string, string>>()
        {
            new ("Server", "Celerio/1.0"),
            new ("Connection", "close"),
            new ("Date", DateTime.UtcNow.ToString("r")),
        };

        foreach (var header in headers)
        {
            if(!resp.Headers.ContainsKey(header.Key))
                resp.Headers.Add(header.Key, header.Value);
        }
    }
    
    public void SendResponse(Stream stream, HttpResponse response)
    {
        var body = Encoding.UTF8.GetBytes(response.Body);
        DefaultHeaders(response); 
        if (!response.Headers.ContainsKey("Content-Length"))
            response.Headers.Add("Content-Length", body.Length.ToString());
        
        if (!response.Headers.ContainsKey("Content-Type"))
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        
        var writer = new StreamWriter(stream);
        writer.WriteLine($"HTTP/1.1 {response.StatusCode} {response.StatusMessage}");
        foreach (var header in response.Headers)
        {
            writer.WriteLine($"{header.Key}: {header.Value}");
        }
        writer.WriteLine();
        writer.WriteLine(response.Body);
        writer.Flush();
    }
}