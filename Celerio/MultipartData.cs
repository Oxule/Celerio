using System.Text;
using System.Text.RegularExpressions;

namespace Celerio;

public class MultipartData
{
    public class Part
    {
        public readonly string Body;
        public readonly byte[] BodyRaw;
        public readonly string Name;
        public readonly string? Filename;
        public readonly HeadersCollection Headers;

        public Part(string body, byte[] bodyRaw, string name, string? filename, HeadersCollection headers)
        {
            Body = body;
            BodyRaw = bodyRaw;
            Name = name;
            Filename = filename;
            Headers = headers;
        }
    }
    
    public readonly Part[] Parts;

    public MultipartData(Part[] parts)
    {
        Parts = parts;
    }
    
    
    private static Regex ContentTypeRegex = new (@"multipart\/form-data; *boundary=(.*)", RegexOptions.Compiled);
    private static Regex ContentDispositionRegex = new (@"form-data; name=\""([^""]*)\""(; filename=\""(.*)\"")?", RegexOptions.Compiled);

    public Part? GetPart(string name) => Parts.FirstOrDefault(p => p.Name == name);
    
    public static bool TryParse(HttpRequest request, out MultipartData? data, out string? reason)
    {
        reason = null;
        data = null;

        if (request.BodyRaw == null || request.BodyRaw.Length == 0)
        {
            reason = "No request body";
            return false;
        }

        if (!request.Headers.TryGet("Content-Type", out var contentType))
        {
            reason = "No Content-Type header";
            return false;
        }

        if (contentType.Count != 1)
        {
            reason = "There should be exactly one Content-Type header";
            return false;
        }

        var contentTypeMatch = ContentTypeRegex.Match(contentType[0]);
        if (!contentTypeMatch.Success)
        {
            reason = "Content-Type header wrong type";
            return false;
        }

        string boundary = "--" + contentTypeMatch.Groups[1].Value;
        byte[] bodyBytes = request.BodyRaw!;
        List<Part> parts = new List<Part>();
        
        int index = 0;
        while (index < bodyBytes.Length)
        {
            var headerStartIndex = IndexOf(bodyBytes, boundary, index)+boundary.Length+2;
            if (headerStartIndex == boundary.Length+1) break;

            int headerEndIndex = IndexOf(bodyBytes, "\r\n\r\n", headerStartIndex);
            if (headerEndIndex == -1) break;
            
            var headersSpan = bodyBytes.AsSpan(headerStartIndex, headerEndIndex - headerStartIndex);
            var headers = ParseHeaders(headersSpan);
            if (!headers.TryGet("Content-Disposition", out var contentDisposition))
            {
                reason = "Every part should contain Content-Disposition header";
                return false;
            }

            if (contentDisposition.Count != 1)
            {
                reason = "Content-Disposition header should contain exactly one value";
                return false;
            }
            var dispositionMatch = ContentDispositionRegex.Match(contentDisposition[0]);
            var name = dispositionMatch.Groups[1].Value; // получить заголовок имени
            string? filename = dispositionMatch.Groups.Count >= 3 ? dispositionMatch.Groups[3].Value : null;

            int bodyStartIndex = headerEndIndex+4;
            
            int bodyEndIndex = IndexOf(bodyBytes, boundary, bodyStartIndex) - 2;
            if (bodyEndIndex == -3) break;
            
            byte[] bodyRaw = bodyBytes.AsSpan(bodyStartIndex, bodyEndIndex - bodyStartIndex).ToArray();
            string? body = Encoding.UTF8.GetString(bodyRaw);

            parts.Add(new Part(body, bodyRaw, name, filename, headers));

            index = bodyEndIndex+2;
        }

        data = new MultipartData(parts.ToArray());
        return true;
    }

    private static int IndexOf(byte[] source, string value, int startIndex = 0)
    {
        byte[] target = Encoding.ASCII.GetBytes(value);
        for (int i = startIndex; i <= source.Length - target.Length; i++)
        {
            int j = 0;
            while (j < target.Length && source[i + j] == target[j])
            {
                j++;
            }
            if (j == target.Length)
            {
                return i;
            }
        }
        return -1;
    }

    private static HeadersCollection ParseHeaders(ReadOnlySpan<byte> headersSpan)
    {
        var headers = new HeadersCollection();
        string headersStr = Encoding.UTF8.GetString(headersSpan);

        foreach (var line in headersStr.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            int colonIndex = line.IndexOf(':');
            if (colonIndex > 0)
            {
                string name = line.Substring(0, colonIndex).Trim();
                string value = line.Substring(colonIndex + 1).Trim();
                headers.Add(name, value);
            }
        }

        return headers;
    }
}