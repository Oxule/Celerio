using System.Globalization;
using System.Net.Sockets;
using System.Text;

namespace Celerio;

public class DefaultHttpProvider : IHttpProvider
{
    private const int MAX_HEADER_SIZE = 8192;

    public bool ParseRequest(NetworkStream stream, out HttpRequest request, out IHttpProvider.HttpParsingError error)
    {
        request = null!;
        error = IHttpProvider.HttpParsingError.None;
        try
        {
            byte[] headerBuffer = new byte[MAX_HEADER_SIZE];
            int totalRead = 0;
            int headerEndIndex = -1;
            byte[] tempBuffer = new byte[1024];

            while (true)
            {
                int bytesRead = stream.Read(tempBuffer, 0, tempBuffer.Length);
                if (bytesRead == 0)
                {
                    error = IHttpProvider.HttpParsingError.IncompleteRequest;
                    return false;
                }

                if (totalRead + bytesRead > MAX_HEADER_SIZE)
                {
                    error = IHttpProvider.HttpParsingError.HeaderTooLarge;
                    return false;
                }

                Buffer.BlockCopy(tempBuffer, 0, headerBuffer, totalRead, bytesRead);
                totalRead += bytesRead;

                headerEndIndex = FindHeaderEnd(headerBuffer, totalRead);
                if (headerEndIndex >= 0)
                    break;
            }

            int headerSectionLength = headerEndIndex + 4;
            string headerText = Encoding.ASCII.GetString(headerBuffer, 0, headerSectionLength);
            string[] headerLines = headerText.Split(new[] { "\r\n" }, StringSplitOptions.None);
            if (headerLines.Length < 1 || string.IsNullOrWhiteSpace(headerLines[0]))
            {
                error = IHttpProvider.HttpParsingError.Syntax;
                return false;
            }

            string requestLine = headerLines[0];
            string[] parts = requestLine.Split(' ');
            if (parts.Length != 3)
            {
                error = IHttpProvider.HttpParsingError.Syntax;
                return false;
            }

            string method = parts[0];
            string uri = parts[1];
            string version = parts[2];
            if (!version.StartsWith("HTTP/", StringComparison.Ordinal))
            {
                error = IHttpProvider.HttpParsingError.Version;
                return false;
            }

            HttpRequest req = new HttpRequest
            {
                Method = method
            };

            int qIndex = uri.IndexOf('?');
            if (qIndex >= 0)
            {
                req.URI = uri.Substring(0, qIndex);
                string queryString = uri.Substring(qIndex + 1);
                foreach (var pair in queryString.Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    int eqIndex = pair.IndexOf('=');
                    if (eqIndex > 0)
                    {
                        string key = Uri.UnescapeDataString(pair.Substring(0, eqIndex));
                        string value = Uri.UnescapeDataString(pair.Substring(eqIndex + 1));
                        req.Query[key] = value;
                    }
                    else
                    {
                        string key = Uri.UnescapeDataString(pair);
                        req.Query[key] = "";
                    }
                }
            }
            else
            {
                req.URI = uri;
            }

            string? currentHeaderName = null;
            StringBuilder currentHeaderValue = new StringBuilder();

            for (int i = 1; i < headerLines.Length; i++)
            {
                string line = headerLines[i];
                if (line.Length == 0)
                    break;

                if ((line[0] == ' ' || line[0] == '\t') && currentHeaderName != null)
                {
                    currentHeaderValue.Append(' ');
                    currentHeaderValue.Append(line.Trim());
                    continue;
                }

                if (currentHeaderName != null)
                {
                    req.Headers.Add(currentHeaderName, currentHeaderValue.ToString());
                }

                int colonIndex = line.IndexOf(':');
                if (colonIndex <= 0)
                {
                    error = IHttpProvider.HttpParsingError.Syntax;
                    return false;
                }

                currentHeaderName = line.Substring(0, colonIndex).Trim();
                currentHeaderValue.Clear();
                string valuePart = line.Substring(colonIndex + 1).Trim();
                currentHeaderValue.Append(valuePart);
            }

            if (currentHeaderName != null)
            {
                req.Headers.Add(currentHeaderName, currentHeaderValue.ToString());
            }

            int leftoverCount = totalRead - headerSectionLength;
            byte[] leftover = new byte[leftoverCount];
            if (leftoverCount > 0)
            {
                Buffer.BlockCopy(headerBuffer, headerSectionLength, leftover, 0, leftoverCount);
            }

            var sb = new StreamBuffer(stream, leftover, leftoverCount);

            string? transferEncoding = req.Headers.GetFirst("Transfer-Encoding");
            if (!string.IsNullOrEmpty(transferEncoding) &&
                transferEncoding.Equals("chunked", StringComparison.OrdinalIgnoreCase))
            {
                using (MemoryStream decodedBody = new MemoryStream())
                {
                    if (!ParseChunkedBody(sb, decodedBody, out error))
                    {
                        return false;
                    }

                    req.Body = decodedBody.ToArray();
                }
            }
            else if (req.Headers.TryGet("Content-Length", out string? contentLengthValue))
            {
                if (!int.TryParse(contentLengthValue, out int contentLength) || contentLength < 0)
                {
                    error = IHttpProvider.HttpParsingError.Syntax;
                    return false;
                }

                byte[] body = new byte[contentLength];
                int alreadyRead = sb.Available;
                if (alreadyRead > contentLength)
                    alreadyRead = contentLength;
                if (alreadyRead > 0)
                {
                    Array.Copy(sb.ReadBuffer, sb.Offset, body, 0, alreadyRead);
                    sb.Skip(alreadyRead);
                }

                int remaining = contentLength - alreadyRead;
                if (remaining > 0)
                {
                    if (!sb.ReadExact(body, alreadyRead, remaining))
                    {
                        error = IHttpProvider.HttpParsingError.IncompleteRequest;
                        return false;
                    }
                }

                req.Body = body;
            }
            else
            {
                if (sb.Available > 0)
                {
                    byte[] body = new byte[sb.Available];
                    Array.Copy(sb.ReadBuffer, sb.Offset, body, 0, sb.Available);
                    sb.Skip(sb.Available);
                    req.Body = body;
                }
                else
                {
                    req.Body = null;
                }
            }

            request = req;
            return true;
        }
        catch (Exception)
        {
            error = IHttpProvider.HttpParsingError.Other;
            request = null!;
            return false;
        }
    }

    private int FindHeaderEnd(byte[] buffer, int length)
    {
        for (int i = 0; i < length - 3; i++)
        {
            if (buffer[i] == 13 && buffer[i + 1] == 10 &&
                buffer[i + 2] == 13 && buffer[i + 3] == 10)
                return i;
        }

        return -1;
    }
    
    private bool ParseChunkedBody(StreamBuffer sb, MemoryStream decoded, out IHttpProvider.HttpParsingError error)
    {
        error = IHttpProvider.HttpParsingError.None;
        while (true)
        {
            string? line = sb.ReadLine();
            if (line == null)
            {
                error = IHttpProvider.HttpParsingError.IncompleteRequest;
                return false;
            }

            int semicolonIndex = line.IndexOf(';');
            string chunkSizeHex = (semicolonIndex >= 0 ? line.Substring(0, semicolonIndex) : line).Trim();
            if (!int.TryParse(chunkSizeHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int chunkSize))
            {
                error = IHttpProvider.HttpParsingError.InvalidChunkSize;
                return false;
            }

            if (chunkSize == 0)
            {
                while (true)
                {
                    string? trailerLine = sb.ReadLine();
                    if (trailerLine == null)
                    {
                        error = IHttpProvider.HttpParsingError.IncompleteRequest;
                        return false;
                    }

                    if (trailerLine.Length == 0)
                        break;
                }

                break;
            }

            byte[] chunkData = new byte[chunkSize];
            if (!sb.ReadExact(chunkData, 0, chunkSize))
            {
                error = IHttpProvider.HttpParsingError.IncompleteRequest;
                return false;
            }

            decoded.Write(chunkData, 0, chunkSize);
            string? emptyLine = sb.ReadLine();
            if (emptyLine == null)
            {
                error = IHttpProvider.HttpParsingError.IncompleteRequest;
                return false;
            }

            if (emptyLine.Length != 0)
            {
                error = IHttpProvider.HttpParsingError.Syntax;
                return false;
            }
        }

        return true;
    }
    
    public async Task SendResponseAsync(NetworkStream stream, HttpResponse response)
    {
        response.PreProcess();

        var sb = new StringBuilder();
        sb.AppendLine($"HTTP/1.1 {response.StatusCode} {response.StatusMessage}");
        foreach (var header in response.Headers)
        {
            foreach (var value in header.Value)
            {
                sb.AppendLine($"{header.Key}: {value}");
            }
        }
        sb.AppendLine();

        var headerBytes = Encoding.ASCII.GetBytes(sb.ToString());
        await stream.WriteAsync(headerBytes, 0, headerBytes.Length).ConfigureAwait(false);

        if (response.Body != null)
        {
            await stream.WriteAsync(response.Body, 0, response.Body.Length).ConfigureAwait(false);
        }

        await stream.FlushAsync().ConfigureAwait(false);
    }}