﻿using System.Net.Sockets;
using System.Text;

namespace Celerio;

public interface IHttpProvider
{
    public bool GetRequest(NetworkStream stream, out HttpRequest request);
    public void SendResponse(NetworkStream stream, HttpResponse response);
}
public class Http11ProtocolProvider : IHttpProvider
{
    public bool GetRequest(NetworkStream stream, out HttpRequest request)
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
                if (request.Headers.Contains(p[0]))
                    return false;
                request.Headers.Add(p[0], string.Join(": ", p.Skip(1)));
            }
            
            pointer += l.Length + 1;
        }
        
        if (request.Headers.TryGet("Content-Length", out var contentLength)&&int.TryParse(contentLength[0], out var length)&&length>0)
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

    private static string? ReadLineStream(NetworkStream stream)
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

    public void SendResponse(NetworkStream stream, HttpResponse response)
    {
        response.PostProcess();
        stream.Write(Encoding.ASCII.GetBytes($"HTTP/1.1 {response.StatusCode} {response.StatusMessage}\n"));
        foreach (var header in response.Headers)
        {
            foreach (var v in header.Value)
            {
                stream.Write(Encoding.ASCII.GetBytes($"{header.Key}: {v}\n"));
            }
        }

        stream.WriteByte((byte)'\n');
        
        if (response.Body != null)
        {
            stream.Write(response.Body, 0, response.Body.Length);
        }
    }
}