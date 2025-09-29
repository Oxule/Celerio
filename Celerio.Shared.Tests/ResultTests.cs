using Celerio;
using Xunit;
using Celerio.Shared;
using System.Text;
using System.IO;
using System.Threading;

public class ResultTests
{
    [Fact]
    public void Constructor_SetStatus()
    {
        var result = new Result(200);
        Assert.Equal(200, result.StatusCode);
        Assert.IsType<BaseResultBody>(result.Body);
    }

    [Fact]
    public void Constructor_WithBody()
    {
        var body = new DefaultResultBody("test");
        var result = new Result(201, body);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal(body, result.Body);
    }

    [Fact]
    public void Constructor_WithHeaders()
    {
        var body = new DefaultResultBody("test");
        var headers = new HeaderCollection();
        var result = new Result(202, body, headers);
        Assert.Equal(202, result.StatusCode);
        Assert.Equal(body, result.Body);
        Assert.Equal(headers, result.Headers);
    }

    [Fact]
    public void Header_AddsHeader()
    {
        var result = new Result(200);
        var chained = result.Header("name", "value");
        Assert.Same(result, chained);
        Assert.Equal("value", result.Headers.Get("name"));
    }

    [Fact]
    public void SetHeader_Sets()
    {
        var result = new Result(200).Header("test", "old").SetHeader("test", "new");
        Assert.Equal("new", result.Headers.Get("test"));
    }

    [Fact]
    public void SetBody_Sets()
    {
        var result = new Result(200);
        var body = new DefaultResultBody("new");
        var chained = result.SetBody(body);
        Assert.Same(result, chained);
        Assert.Equal(body, result.Body);
    }

    [Fact]
    public void FactoryMethods()
    {
        var ok = Result.Ok();
        Assert.Equal(200, ok.StatusCode);

        var created = Result.Created("/new");
        Assert.Equal(201, created.StatusCode);
        Assert.Equal("/new", created.Headers.Get("Location"));

        var notFound = Result.NotFound();
        Assert.Equal(404, notFound.StatusCode);
    }

    [Fact]
    public void Header_SetCookie()
    {
        var result = new Result(200).Header("Set-Cookie", "session=abc");
        Assert.Equal("session=abc", result.Headers.Get("Set-Cookie"));
    }

    [Fact]
    public void NoContent_Test()
    {
        var result = Result.NoContent();
        Assert.Equal(204, result.StatusCode);
    }

    [Fact]
    public void Accepted_Test()
    {
        var result = Result.Accepted();
        Assert.Equal(202, result.StatusCode);
    }

    [Fact]
    public void MovedPermanently_Test()
    {
        var location = "/new-location";
        var result = Result.MovedPermanently(location);
        Assert.Equal(301, result.StatusCode);
        Assert.Equal(location, result.Headers.Get("Location"));
    }

    [Fact]
    public void Found_Test()
    {
        var location = "/temp-location";
        var result = Result.Found(location);
        Assert.Equal(302, result.StatusCode);
        Assert.Equal(location, result.Headers.Get("Location"));
    }

    [Fact]
    public void SeeOther_Test()
    {
        var location = "/other";
        var result = Result.SeeOther(location);
        Assert.Equal(303, result.StatusCode);
        Assert.Equal(location, result.Headers.Get("Location"));
    }

    [Fact]
    public void TemporaryRedirect_Test()
    {
        var location = "/temp-redirect";
        var result = Result.TemporaryRedirect(location);
        Assert.Equal(307, result.StatusCode);
        Assert.Equal(location, result.Headers.Get("Location"));
    }

    [Fact]
    public void PermanentRedirect_Test()
    {
        var location = "/perm-redirect";
        var result = Result.PermanentRedirect(location);
        Assert.Equal(308, result.StatusCode);
        Assert.Equal(location, result.Headers.Get("Location"));
    }

    [Fact]
    public void BadRequest_Test()
    {
        var result = Result.BadRequest();
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void Unauthorized_Test()
    {
        var result = Result.Unauthorized();
        Assert.Equal(401, result.StatusCode);
    }

    [Fact]
    public void Forbidden_Test()
    {
        var result = Result.Forbidden();
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public void Conflict_Test()
    {
        var result = Result.Conflict();
        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public void Gone_Test()
    {
        var result = Result.Gone();
        Assert.Equal(410, result.StatusCode);
    }

    [Fact]
    public void UnsupportedMediaType_Test()
    {
        var result = Result.UnsupportedMediaType();
        Assert.Equal(415, result.StatusCode);
    }

    [Fact]
    public void UnprocessableEntity_Test()
    {
        var result = Result.UnprocessableEntity();
        Assert.Equal(422, result.StatusCode);
    }

    [Fact]
    public void InternalServerError_Test()
    {
        var result = Result.InternalServerError();
        Assert.Equal(500, result.StatusCode);
    }

    [Fact]
    public void NotImplemented_Test()
    {
        var result = Result.NotImplemented();
        Assert.Equal(501, result.StatusCode);
    }

    [Fact]
    public void BadGateway_Test()
    {
        var result = Result.BadGateway();
        Assert.Equal(502, result.StatusCode);
    }

    [Fact]
    public void ServiceUnavailable_Test()
    {
        var result = Result.ServiceUnavailable();
        Assert.Equal(503, result.StatusCode);
    }

    [Fact]
    public void GatewayTimeout_Test()
    {
        var result = Result.GatewayTimeout();
        Assert.Equal(504, result.StatusCode);
    }

    [Fact]
    public async Task WriteResultAsync_WritesCorrectHttpResponse()
    {
        using var conn = NetworkStreamTestHelper.CreateLoopbackConnection();
        var clientStream = conn.ClientStream;
        var serverStream = conn.ServerStream;
        {
            var result = new Result(200);

            await result.WriteResultAsync(serverStream);
            serverStream.Close();

            var reader = new StreamReader(clientStream, Encoding.ASCII);
            var response = await reader.ReadToEndAsync();

            var lines = response.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            Assert.Equal("HTTP/1.1 200 OPT", lines[0]);
            Assert.StartsWith("Date: ", lines[1]);
            Assert.Equal("Server: Celerio/2.0", lines[2]);
            Assert.Equal("Content-Length: 0", lines[3]);
            Assert.Equal("", lines[4]);
        }
    }
}
