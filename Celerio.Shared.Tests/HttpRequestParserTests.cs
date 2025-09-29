using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Celerio;

public class HttpRequestParserTests {

    private async Task<(TcpClient server, TcpClient client)> CreateServerClientWithData(string requestData)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var dataBytes = Encoding.ASCII.GetBytes(requestData);
        var acceptTask = listener.AcceptTcpClientAsync();
        var client = new TcpClient();
        var localEndPoint = (IPEndPoint)listener.LocalEndpoint;
        client.Connect(localEndPoint.Address, localEndPoint.Port);
        var server = await acceptTask;

        var stream = client.GetStream();
        await stream.WriteAsync(dataBytes, 0, dataBytes.Length);
        stream.Flush();

        listener.Stop();
        return (server, client);
    }

    [Fact]
    public void ParseAsync_NullNetworkStream_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(async () => await HttpRequestParser.ParseAsync(null));
    }

    [Fact]
    public async Task ParseAsync_ValidGetRequest_NoBody_Success()
    {
        const string requestData = "GET /path HTTP/1.1\r\nHost: example.com\r\n\r\n";
        using var conn = NetworkStreamTestHelper.WriteNetworkStream(Encoding.ASCII.GetBytes(requestData));

        var request = await HttpRequestParser.ParseAsync(conn.ServerStream);

        Assert.Equal("GET", request.Method);
        Assert.Equal("/path", request.Path);
        Assert.Equal(0, request.Body.Length);
        Assert.Equal("example.com", request.Headers.Get("Host"));
    }

    [Fact]
    public async Task ParseAsync_PostWithContentLength_Success()
    {
        const string body = "test body data";
        var requestData = $"POST /submit HTTP/1.1\r\nHost: example.com\r\nContent-Length: {body.Length}\r\n\r\n{body}";
        using var conn = NetworkStreamTestHelper.WriteNetworkStream(Encoding.ASCII.GetBytes(requestData));

        var request = await HttpRequestParser.ParseAsync(conn.ServerStream);

        Assert.Equal("POST", request.Method);
        Assert.Equal("/submit", request.Path);
        Assert.Equal(body, Encoding.UTF8.GetString(request.Body));
    }

    [Fact]
    public async Task ParseAsync_PostChunked_Success()
    {
        const string body = "Wikipedia in\r\n\r\nchunks.";
        var chunk1 = "Wikipedia";
        var chunk2 = " in\r\n\r\nchunks.";
        var chunkedData = $"POST /test HTTP/1.1\r\nHost: example.com\r\nTransfer-Encoding: chunked\r\n\r\n" +
                          $"{chunk1.Length:X}\r\n{chunk1}\r\n{chunk2.Length:X}\r\n{chunk2}\r\n0\r\n\r\n";
        using var conn = NetworkStreamTestHelper.WriteNetworkStream(Encoding.ASCII.GetBytes(chunkedData));

        var request = await HttpRequestParser.ParseAsync(conn.ServerStream);

        Assert.Equal("POST", request.Method);
        Assert.Equal("/test", request.Path);
        Assert.Equal(body, Encoding.UTF8.GetString(request.Body));
    }

    [Fact]
    public void ParseAsync_EmptyRequest_ThrowsFormatException()
    {
        const string requestData = "";
        Assert.ThrowsAsync<FormatException>(async () =>
        {
            using var conn = NetworkStreamTestHelper.WriteNetworkStream(Encoding.ASCII.GetBytes(requestData));
            await HttpRequestParser.ParseAsync(conn.ServerStream);
        });
    }

    [Fact]
    public void ParseAsync_MalformedRequestLine_ThrowsFormatException()
    {
        const string requestData = "GET /something\r\n\r\n"; // Missing HTTP version
        Assert.ThrowsAsync<FormatException>(async () =>
        {
            var (server, client) = await CreateServerClientWithData(requestData);
            using var ns = (NetworkStream)server.GetStream();
            using var _client = client;
            using var _server = server;
            await HttpRequestParser.ParseAsync(ns);
        });
    }

    [Fact]
    public void ParseAsync_MalformedRequestLineTooManyParts_ThrowsFormatException()
    {
        const string requestData = "GET /path HTTP/1.1 extra\r\n\r\n";
        Assert.ThrowsAsync<FormatException>(async () =>
        {
            var (server, client) = await CreateServerClientWithData(requestData);
            using var ns = (NetworkStream)server.GetStream();
            using var _client = client;
            using var _server = server;
            await HttpRequestParser.ParseAsync(ns);
        });
    }

    [Fact]
    public async Task ParseAsync_RequestWithQueryParameters_Success()
    {
        const string requestData = "GET /path?key1=value1&key2=value2 HTTP/1.1\r\nHost: example.com\r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        using var _client = client;
        using var _server = server;

        var request = await HttpRequestParser.ParseAsync(ns);

        Assert.Equal("GET", request.Method);
        Assert.Equal("/path", request.Path);
        Assert.Equal(2, request.Query.Count);
        Assert.Equal("value1", request.Query["key1"]);
        Assert.Equal("value2", request.Query["key2"]);
    }

    [Fact]
    public async Task ParseAsync_RequestWithEncodedQueryParameters_Success()
    {
        const string requestData = "GET /path?key%201=val%20a&key2= HTTP/1.1\r\nHost: example.com\r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        using var _client = client;
        using var _server = server;

        var request = await HttpRequestParser.ParseAsync(ns);

        Assert.Equal("val a", request.Query["key 1"]);
        Assert.Equal("", request.Query["key2"]);
        Assert.True(request.Query.ContainsKey("key2"));
    }

    [Fact]
    public void ParseAsync_RequestWithInvalidPercentEncodingInQuery_ThrowsFormatException()
    {
        const string requestData = "GET /path?key=%INVALID HTTP/1.1\r\nHost: example.com\r\n\r\n";
        Assert.ThrowsAsync<FormatException>(async () =>
        {
            var (server, client) = await CreateServerClientWithData(requestData);
            using var ns = (NetworkStream)server.GetStream();
            using var _client = client;
            using var _server = server;
            await HttpRequestParser.ParseAsync(ns);
        });
    }

    [Fact]
    public async Task ParseAsync_RequestWithMultipleHeaders_Success()
    {
        const string requestData = "GET /test HTTP/1.1\r\nHost: example.com\r\nUser-Agent: TestAgent\r\nContent-Type: text/plain\r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        using var _client = client;
        using var _server = server;

        var request = await HttpRequestParser.ParseAsync(ns);

        Assert.Equal("example.com", request.Headers.Get("Host"));
        Assert.Equal("TestAgent", request.Headers.Get("User-Agent"));
        Assert.Equal("text/plain", request.Headers.Get("Content-Type"));
    }

    [Fact]
    public async Task ParseAsync_RequestWithMalformedHeader_SkipsHeader()
    {
        const string requestData = "GET /test HTTP/1.1\r\nHost example.com\r\n\r\n"; // No colon
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        using var _client = client;
        using var _server = server;

        var request = await HttpRequestParser.ParseAsync(ns);

        Assert.Null(request.Headers.Get("Host"));
    }

    [Fact]
    public void ParseAsync_HeadersTooLarge_ThrowsFormatException()
    {
        var largeHeaders = new string('a', 5 * 1024 * 1024); // Over 4MB
        var requestData = $"GET /test HTTP/1.1\r\n{largeHeaders}\r\n\r\n";
        Assert.ThrowsAsync<FormatException>(async () =>
        {
            var (server, client) = await CreateServerClientWithData(requestData);
            using var ns = (NetworkStream)server.GetStream();
            using var _client = client;
            using var _server = server;
            await HttpRequestParser.ParseAsync(ns);
        });
    }

    [Fact]
    public void ParseAsync_PostWithContentLengthLargerThanData()
    {
        const string body = "short";
        var requestData = $"POST /test HTTP/1.1\r\nContent-Length: 100\r\n\r\n{body}"; // Content-Length > actual
        Assert.ThrowsAsync<IOException>(async () =>
        {
            var (server, client) = await CreateServerClientWithData(requestData);
            using var ns = (NetworkStream)server.GetStream();
            using var _client = client;
            using var _server = server;
            await HttpRequestParser.ParseAsync(ns);
        });
    }

    [Fact]
    public void ParseAsync_PostChunkedWithInvalidChunkSize_ThrowsFormatException()
    {
        var requestData = $"POST /test HTTP/1.1\r\nTransfer-Encoding: chunked\r\n\r\nINVALID\r\nchunk\r\n0\r\n\r\n";
        Assert.ThrowsAsync<FormatException>(async () =>
        {
            var (server, client) = await CreateServerClientWithData(requestData);
            using var ns = (NetworkStream)server.GetStream();
            using var _client = client;
            using var _server = server;
            await HttpRequestParser.ParseAsync(ns);
        });
    }

    [Fact]
    public async Task ParseAsync_PostChunkedWithTrailers_Success()
    {
        const string body = "test data";
        var requestData = $"POST /test HTTP/1.1\r\nTransfer-Encoding: chunked\r\n\r\n{body.Length:X}\r\n{body}\r\n0\r\nTrailer: value\r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        using var _client = client;
        using var _server = server;

        var request = await HttpRequestParser.ParseAsync(ns);

        Assert.Equal(body, Encoding.UTF8.GetString(request.Body));
    }

    [Fact]
    public async Task ParseAsync_PostChunkedWithZeroChunk_Success()
    {
        var requestData = $"POST /test HTTP/1.1\r\nTransfer-Encoding: chunked\r\n\r\n0\r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        using var _client = client;
        using var _server = server;

        var request = await HttpRequestParser.ParseAsync(ns);

        Assert.Equal(0, request.Body.Length);
    }

    [Fact]
    public async Task ParseAsync_PostWithLeftoverFromHeaders_Success()
    {
        const string body = "0123456789abcdef";
        var requestData = $"POST /test HTTP/1.1\r\nContent-Length: {body.Length}\r\n\r\n0123"; // Leftover 4 bytes
        var extendedData = "456789abcdef"; // Additional data to simulate network
        var (server, client) = await CreateServerClientWithData(requestData.Replace("0123", body));
        using var ns = (NetworkStream)server.GetStream();
        using var _client = client;
        using var _server = server;

        var request = await HttpRequestParser.ParseAsync(ns);

        Assert.Equal(body, Encoding.UTF8.GetString(request.Body));
    }

    [Fact]
    public async Task ParseAsync_PostWithMultipleTransferEncodingChunked_Success()
    {
        const string body = "data";
        var requestData = $"POST /test HTTP/1.1\r\nTransfer-Encoding: gzip, chunked\r\n\r\n{body.Length:X}\r\n{body}\r\n0\r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        using var _client = client;
        using var _server = server;

        var request = await HttpRequestParser.ParseAsync(ns);

        Assert.Equal(body, Encoding.UTF8.GetString(request.Body));
    }

    [Fact]
    public async Task ParseAsync_RequestWithPathEncoding_Success()
    {
        const string requestData = "GET /path%20with%20spaces HTTP/1.1\r\nHost: example.com\r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        using var _client = client;
        using var _server = server;

        var request = await HttpRequestParser.ParseAsync(ns);

        Assert.Equal("/path with spaces", request.Path);
    }

    [Fact]
    public void ParseAsync_InvalidEncodingInPath_ThrowsFormatException()
    {
        const string requestData = "GET /path%ZZ HTTP/1.1\r\nHost: example.com\r\n\r\n";
        Assert.ThrowsAsync<FormatException>(async () =>
        {
            var (server, client) = await CreateServerClientWithData(requestData);
            using var ns = (NetworkStream)server.GetStream();
            using var _client = client;
            using var _server = server;
            await HttpRequestParser.ParseAsync(ns);
        });
    }

    [Fact]
    public void ParseAsync_WithCancellation_ThrowsTaskCanceledException()
    {
        const string requestData = "GET /path HTTP/1.1\r\nHost: example.com\r\n\r\n";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            var (server, client) = await CreateServerClientWithData(requestData);
            using var ns = (NetworkStream)server.GetStream();
            using var _client = client;
            using var _server = server;
            await HttpRequestParser.ParseAsync(ns, cts.Token);
        });
    }

    [Fact]
    public async Task ParseAsync_QueryWithEmptyValues_Success()
    {
        const string requestData = "GET /path?key1=&&key2&key3=%20 HTTP/1.1\r\nHost: example.com\r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        using var _client = client;
        using var _server = server;

        var request = await HttpRequestParser.ParseAsync(ns);

        Assert.Equal("", request.Query["key1"]);
        Assert.True(request.Query.ContainsKey("key2"));
        Assert.Equal("", request.Query["key2"]);
        Assert.Equal(" ", request.Query["key3"]);
    }

    [Fact]
    public async Task ParseAsync_PostWithZeroContentLength_Success()
    {
        var requestData = $"POST /test HTTP/1.1\r\nContent-Length: 0\r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        using var _client = client;
        using var _server = server;

        var request = await HttpRequestParser.ParseAsync(ns);

        Assert.Equal("POST", request.Method);
        Assert.Equal(0, request.Body.Length);
    }
    
    [Fact]
    public async Task ParseAsync_GetRequestWithDifferentMethods_Success()
    {
        var methods = new[] { "HEAD", "PUT", "DELETE", "OPTIONS", "PATCH" };
        foreach (var method in methods)
        {
            var requestData = $"{method} /path HTTP/1.1\r\nHost: example.com\r\n\r\n";
            using var conn = NetworkStreamTestHelper.WriteNetworkStream(Encoding.ASCII.GetBytes(requestData));
            var request = await HttpRequestParser.ParseAsync(conn.ServerStream);
            Assert.Equal(method, request.Method);
        }
    }

    [Fact]
    public async Task ParseAsync_RequestWithNoQuery_Success()
    {
        const string requestData = "GET /path HTTP/1.1\r\nHost: example.com\r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        var request = await HttpRequestParser.ParseAsync(ns);
        Assert.Equal("/path", request.Path);
        Assert.Empty(request.Query);
    }

    [Fact]
    public async Task ParseAsync_QueryWithAmpersandOnly_Success()
    {
        const string requestData = "GET /path?& HTTP/1.1\r\nHost: example.com\r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        var request = await HttpRequestParser.ParseAsync(ns);
        Assert.Equal("/path", request.Path);
        Assert.Empty(request.Query);
    }

    [Fact]
    public async Task ParseAsync_QueryWithEqualsOnly_Success()
    {
        const string requestData = "GET /path?= HTTP/1.1\r\nHost: example.com\r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        var request = await HttpRequestParser.ParseAsync(ns);
        Assert.Equal("/path", request.Path);
        Assert.Single(request.Query);
        Assert.Equal("", request.Query[""]);
    }

    [Fact]
    public async Task ParseAsync_HeaderWithSpaceBeforeColon_Success()
    {
        const string requestData = "GET /path HTTP/1.1\r\nHost : example.com\r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        var request = await HttpRequestParser.ParseAsync(ns);
        Assert.Equal("example.com", request.Headers.Get("host")); // assuming case insensitive
    }

    [Fact]
    public async Task ParseAsync_HeaderWithMultipleSpaces_Success()
    {
        const string requestData = "GET /path HTTP/1.1\r\nHost  :  example.com  \r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        var request = await HttpRequestParser.ParseAsync(ns);
        Assert.Equal("example.com", request.Headers.Get("host"));
    }

    [Fact]
    public async Task ParseAsync_HeaderWithEmptyValue_Success()
    {
        const string requestData = "GET /path HTTP/1.1\r\nHost: \r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        var request = await HttpRequestParser.ParseAsync(ns);
        Assert.Equal("", request.Headers.Get("Host"));
    }

    [Fact]
    public async Task ParseAsync_MultipleHeadersWithSameName_Success()
    {
        const string requestData = "GET /path HTTP/1.1\r\nSet-Cookie: a=b\r\nSet-Cookie: c=d\r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        var request = await HttpRequestParser.ParseAsync(ns);
        if (request.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            Assert.Equal(new[] { "a=b", "c=d" }, cookies);
        }
        else
        {
            Assert.Fail("Set-Cookie headers not found");
        }
    }

    [Fact]
    public async Task ParseAsync_PostWithContentLengthNegative_ThrowsFormatException()
    {
        var requestData = $"POST /test HTTP/1.1\r\nContent-Length: -5\r\n\r\nbody";
        Assert.ThrowsAsync<FormatException>(async () =>
        {
            var (server, client) = await CreateServerClientWithData(requestData);
            using var ns = (NetworkStream)server.GetStream();
            using var _client = client;
            using var _server = server;
            await HttpRequestParser.ParseAsync(ns);
        });
    }

    [Fact]
    public async Task ParseAsync_PostWithContentLengthNotNumber_ThrowsFormatException()
    {
        var requestData = $"POST /test HTTP/1.1\r\nContent-Length: abc\r\n\r\nbody";
        Assert.ThrowsAsync<FormatException>(async () =>
        {
            var (server, client) = await CreateServerClientWithData(requestData);
            using var ns = (NetworkStream)server.GetStream();
            using var _client = client;
            using var _server = server;
            await HttpRequestParser.ParseAsync(ns);
        });
    }

    [Fact]
    public async Task ParseAsync_PostWithContentLengthTooLarge_IOExceptionEventually()
    {
        var requestData = $"POST /test HTTP/1.1\r\nContent-Length: {int.MaxValue}\r\n\r\nshortbody";
        Assert.ThrowsAsync<IOException>(async () =>
        {
            var (server, client) = await CreateServerClientWithData(requestData);
            using var ns = (NetworkStream)server.GetStream();
            using var _client = client;
            using var _server = server;
            await HttpRequestParser.ParseAsync(ns);
        });
    }

    [Fact]
    public async Task ParseAsync_GetWithEmptyPath_Success()
    {
        const string requestData = "GET / HTTP/1.1\r\nHost: example.com\r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        var request = await HttpRequestParser.ParseAsync(ns);
        Assert.Equal("/", request.Path);
    }

    [Fact]
    public async Task ParseAsync_GetWithComplexPath_Success()
    {
        const string requestData = "GET /api/v1/users/123 HTTP/1.1\r\nHost: example.com\r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        var request = await HttpRequestParser.ParseAsync(ns);
        Assert.Equal("/api/v1/users/123", request.Path);
    }

    [Fact]
    public async Task ParseAsync_PostWithLargeBody_Success()
    {
        var largeBody = new string('x', 1024 * 10);
        var requestData = $"POST /test HTTP/1.1\r\nContent-Length: {largeBody.Length}\r\n\r\n{largeBody}";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        var request = await HttpRequestParser.ParseAsync(ns);
        Assert.Equal(largeBody, Encoding.UTF8.GetString(request.Body));
    }

    [Fact]
    public async Task ParseAsync_PathWithSpecialCharsEncoded_Success()
    {
        const string requestData = "GET /path%2Bwith%21special HTTP/1.1\r\nHost: example.com\r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        var request = await HttpRequestParser.ParseAsync(ns);
        Assert.Equal("/path+with!special", request.Path);
    }

    [Fact]
    public async Task ParseAsync_PathWithUnicodeEncoded_Success()
    {
        const string requestData = "GET /path%C3%A9 HTTP/1.1\r\nHost: example.com\r\n\r\n"; // é
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        var request = await HttpRequestParser.ParseAsync(ns);
        Assert.Equal("/pathé", request.Path);
    }

    [Fact]
    public void ParseAsync_PathWithIncompleteEncoding_ThrowsFormatException()
    {
        const string requestData = "GET /path%E HTTP/1.1\r\nHost: example.com\r\n\r\n"; // Incomplete %
        Assert.ThrowsAsync<FormatException>(async () =>
        {
            var (server, client) = await CreateServerClientWithData(requestData);
            using var ns = (NetworkStream)server.GetStream();
            using var _client = client;
            using var _server = server;
            await HttpRequestParser.ParseAsync(ns);
        });
    }

    [Fact]
    public void ParseAsync_QueryWithIncompleteEncoding_ThrowsFormatException()
    {
        const string requestData = "GET /path?key=%Ax HTTP/1.1\r\nHost: example.com\r\n\r\n";
        Assert.ThrowsAsync<FormatException>(async () =>
        {
            var (server, client) = await CreateServerClientWithData(requestData);
            using var ns = (NetworkStream)server.GetStream();
            using var _client = client;
            using var _server = server;
            await HttpRequestParser.ParseAsync(ns);
        });
    }

    [Fact]
    public async Task ParseAsync_RequestWithOnlyCRLF_ThrowsFormatException()
    {
        const string requestData = "\r\n\r\n";
        Assert.ThrowsAsync<FormatException>(async () =>
        {
            var (server, client) = await CreateServerClientWithData(requestData);
            using var ns = (NetworkStream)server.GetStream();
            using var _client = client;
            using var _server = server;
            await HttpRequestParser.ParseAsync(ns);
        });
    }

    [Fact]
    public async Task ParseAsync_RequestLineWithExtraSpaces_ThrowsFormatException()
    {
        const string requestData = "GET    /path    HTTP/1.1\r\n\r\n";
        // Code splits by space, so "GET", "", "", "", "/path", "", "", "", "HTTP/1.1" - more than 3 parts
        Assert.ThrowsAsync<FormatException>(async () =>
        {
            var (server, client) = await CreateServerClientWithData(requestData);
            using var ns = (NetworkStream)server.GetStream();
            using var _client = client;
            using var _server = server;
            await HttpRequestParser.ParseAsync(ns);
        });
    }

    [Fact]
    public async Task ParseAsync_RequestWithEmptyHeaders_Success()
    {
        const string requestData = "GET /path HTTP/1.1\r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        var request = await HttpRequestParser.ParseAsync(ns);
        Assert.Empty(request.Headers);
    }

    [Fact]
    public async Task ParseAsync_RequestWithHeaderNoValue_Skipped()
    {
        const string requestData = "GET /path HTTP/1.1\r\nHost:\r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        var request = await HttpRequestParser.ParseAsync(ns);
        Assert.Equal("", request.Headers.Get("Host"));
    }

    [Fact]
    public async Task ParseAsync_RequestWithHeaderNoName_Skipped()
    {
        const string requestData = "GET /path HTTP/1.1\r\n: value\r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        var request = await HttpRequestParser.ParseAsync(ns);
        Assert.Null(request.Headers.Get(""));
    }

    [Fact]
    public async Task ParseAsync_PostWithInvalidContentLengthValue_ThrowsFormatException()
    {
        var requestData = $"POST /test HTTP/1.1\r\nContent-Length: abc def \r\n\r\nbody";
        Assert.ThrowsAsync<FormatException>(async () =>
        {
            var (server, client) = await CreateServerClientWithData(requestData);
            using var ns = (NetworkStream)server.GetStream();
            using var _client = client;
            using var _server = server;
            await HttpRequestParser.ParseAsync(ns);
        });
    }

    [Fact]
    public async Task ParseAsync_PostChunkedCaseInsensitive_Success()
    {
        const string body = "data";
        var requestData = $"POST /test HTTP/1.1\r\ntransfer-encoding: CHUNKED\r\n\r\n{body.Length:X}\r\n{body}\r\n0\r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        var request = await HttpRequestParser.ParseAsync(ns);
        Assert.Equal(body, Encoding.UTF8.GetString(request.Body));
    }

    [Fact]
    public async Task ParseAsync_PostChunkedWithSemicolon_NoValue_Success()
    {
        const string body = "data";
        var requestData = $"POST /test HTTP/1.1\r\nTransfer-Encoding: chunked;\r\n\r\n{body.Length:X};comment\r\n{body}\r\n0\r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        var request = await HttpRequestParser.ParseAsync(ns);
        Assert.Equal(body, Encoding.UTF8.GetString(request.Body));
    }
    
    [Fact]
    public async Task ParseAsync_QueryWithPercentEncoded_Name_Success()
    {
        const string requestData = "GET /path?ke%79=value HTTP/1.1\r\nHost: example.com\r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        var request = await HttpRequestParser.ParseAsync(ns);
        Assert.Equal("value", request.Query["key"]);
    }

    [Fact]
    public async Task ParseAsync_QueryWithMultipleEquals_Success()
    {
        const string requestData = "GET /path?key=value=extra HTTP/1.1\r\nHost: example.com\r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        var request = await HttpRequestParser.ParseAsync(ns);
        Assert.Equal("value=extra", request.Query["key"]);
    }

    [Fact]
    public async Task ParseAsync_QueryWithEncodedAmpersand_Success()
    {
        const string requestData = "GET /path?key=val%26other HTTP/1.1\r\nHost: example.com\r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        var request = await HttpRequestParser.ParseAsync(ns);
        Assert.Equal("val&other", request.Query["key"]);
    }

    [Fact]
    public async Task ParseAsync_PathWithEncodedSlash_Success()
    {
        const string requestData = "GET /path%2Fwith%2Fslashes HTTP/1.1\r\nHost: example.com\r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        var request = await HttpRequestParser.ParseAsync(ns);
        Assert.Equal("/path/with/slashes", request.Path);
    }

    [Fact]
    public void ParseAsync_PathWithInvalidCharAfterPercent_ThrowsFormatException()
    {
        const string requestData = "GET /path%G1 HTTP/1.1\r\nHost: example.com\r\n\r\n";
        Assert.ThrowsAsync<FormatException>(async () =>
        {
            var (server, client) = await CreateServerClientWithData(requestData);
            using var ns = (NetworkStream)server.GetStream();
            using var _client = client;
            using var _server = server;
            await HttpRequestParser.ParseAsync(ns);
        });
    }

    [Fact]
    public async Task ParseAsync_HeaderWithTab_Success()
    {
        const string requestData = "GET /path HTTP/1.1\r\nHost:\texample.com\r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        var request = await HttpRequestParser.ParseAsync(ns);
        Assert.Equal("example.com", request.Headers.Get("Host"));
    }

    [Fact]
    public async Task ParseAsync_MultipleEmptyLines_Success()
    {
        const string requestData = "GET /path HTTP/1.1\r\n\r\n\r\n";
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        var request = await HttpRequestParser.ParseAsync(ns);
        Assert.Equal("/path", request.Path);
    }

    [Fact]
    public async Task ParseAsync_PostChunkedWithUpperHex_Success()
    {
        const string body = "test";
        var requestData = $"POST /test HTTP/1.1\r\nTransfer-Encoding: chunked\r\n\r\n{body.Length:X}\r\n{body}\r\n0\r\n\r\n"; // Upper case hex
        var (server, client) = await CreateServerClientWithData(requestData);
        using var ns = (NetworkStream)server.GetStream();
        var request = await HttpRequestParser.ParseAsync(ns);
        Assert.Equal(body, Encoding.UTF8.GetString(request.Body));
    }
}
