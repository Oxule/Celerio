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
        
        if (request.Headers.TryGetValue("Content-Length", out var contentLength)&&int.TryParse(contentLength, out var length)&&length>0)
        {
            byte[] buffer = new byte[length];
            if (stream.Read(buffer, 0, length) != length)
                return false;
            request.BodyRaw = buffer;
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

            if (b == '%')
            {
                var hbyte = new byte[2];
                stream.Read(hbyte, 0, 2);
                buffer.Add(HexToByte(hbyte));
                continue;
            }
            buffer.Add((byte)b);
        }
        
        return Encoding.UTF8.GetString(buffer.ToArray()).Trim();
    }

    private static byte HexToByte(byte[] chars)
    {
        if(chars.Length != 2)
            throw new ArgumentException("Input HEX not a byte");
        byte a = HexCharToByte(chars[0]);
        byte b = HexCharToByte(chars[1]);
        
        return (byte)(a*16+b);
    }

    private static byte HexCharToByte(byte c)
    {
        switch (c)
        {
            case (byte)'0':
                return 0;
            case (byte)'1':
                return 1;
            case (byte)'2':
                return 2;
            case (byte)'3':
                return 3;
            case (byte)'4':
                return 4;
            case (byte)'5':
                return 5;
            case (byte)'6':
                return 6;
            case (byte)'7':
                return 7;
            case (byte)'8':
                return 8;
            case (byte)'9':
                return 9;
            case (byte)'A':
                return 10;
            case (byte)'B':
                return 11;
            case (byte)'C':
                return 12;
            case (byte)'D':
                return 13;
            case (byte)'E':
                return 14;
            case (byte)'F':
                return 15;
        }
        throw new ArgumentException("Input HEX not a byte");
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
        byte[] body;
        if(response.BodyRaw != null && response.BodyRaw.Length > 0)
            body = response.BodyRaw;
        else
            body = Encoding.UTF8.GetBytes(response.Body);
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
        writer.Flush();
        stream.Write(body, 0, body.Length);
        stream.Flush();
    }
}