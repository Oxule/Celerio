using Xunit;
using Celerio.Shared;
using System.Text.Json;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Celerio;

public class ResultBodyTests
{
    [Fact]
    public async Task BaseResultBody_WriteHeaders_DoesNothingSpecified()
    {
        var baseBody = new BaseResultBody();
        using var ms = new MemoryStream();
        using var sw = new StreamWriter(ms);
        await baseBody.WriteBodyHeadersAsync(sw);
        await sw.FlushAsync();
        var output = Encoding.ASCII.GetString(ms.ToArray());
        Assert.Contains("Content-Length: 0", output);
    }

    [Fact]
    public void DefaultResultBody_ByteArrayConstructor()
    {
        var data = new byte[] { 1, 2, 3 };
        var body = new DefaultResultBody(data, "custom/type");
        Assert.Equal(data, body.Body);
        Assert.Equal("custom/type", body.ContentType);
    }

    [Fact]
    public void DefaultResultBody_StringConstructor()
    {
        var body = new DefaultResultBody("hello", "text/test");
        Assert.Equal(Encoding.UTF8.GetBytes("hello"), body.Body);
        Assert.Equal("text/test;charset=utf-8", body.ContentType);
    }

    [Fact]
    public void DefaultResultBody_DefaultContentType()
    {
        var body = new DefaultResultBody(new byte[0]);
        Assert.Equal("application/octet-stream", body.ContentType);
    }

    [Fact]
    public async Task WriteBodyHeaders_IncludesLengthAndType()
    {
        var body = new DefaultResultBody("test", "text/plain");
        using var ms = new MemoryStream();
        using var sw = new StreamWriter(ms);
        await body.WriteBodyHeadersAsync(sw);
        await sw.FlushAsync();
        var output = Encoding.ASCII.GetString(ms.ToArray());
        Assert.Contains("Content-Length: 4", output); // "test" is 4 bytes
        Assert.Contains("Content-Type: text/plain;charset=utf-8", output);
    }

    [Fact]
    public void Body_Text()
    {
        var textBody = Body.Text("sample");
        Assert.Equal("sample", Encoding.UTF8.GetString(textBody.Body));
        Assert.Contains("text/plain", textBody.ContentType);
    }

    [Fact]
    public void Body_Json_SerializesObject()
    {
        var obj = new { id = 1, name = "test" };
        var jsonBody = Body.Json(obj);
        var json = Encoding.UTF8.GetString(jsonBody.Body);
        Assert.Contains("\"id\":1", json);
        Assert.Contains("\"name\":\"test\"", json);
    }

    [Fact]
    public void Body_Html()
    {
        var htmlBody = Body.Html("<p>hi</p>");
        Assert.Equal("<p>hi</p>", Encoding.UTF8.GetString(htmlBody.Body));
        Assert.Contains("text/html;", htmlBody.ContentType);
    }

    [Fact]
    public void ResultExtensions_Text()
    {
        var result = new Result(200);
        var newResult = result.Text("content");
        Assert.Same(result, newResult);
        Assert.IsType<DefaultResultBody>(result.Body);
    }

    [Fact]
    public async Task DefaultResultBody_ByteArray_WriteBodyHeaders_IncludesLengthAndCustomType()
    {
        var body = new DefaultResultBody(new byte[] { 1, 2, 3 }, "image/png");

        using var ms = new MemoryStream();
        using var sw = new StreamWriter(ms);
        await body.WriteBodyHeadersAsync(sw);
        await sw.FlushAsync();
        var output = Encoding.ASCII.GetString(ms.ToArray());
        Assert.Contains("Content-Length: 3", output);
        Assert.Contains("Content-Type: image/png", output);
    }

    [Fact]
    public void Body_Json_ComplexObject_SerializesCorrectly()
    {
        var obj = new { nested = new { key = "value" }, list = new List<int> { 1, 2 } };
        var jsonBody = Body.Json(obj);
        var json = Encoding.UTF8.GetString(jsonBody.Body);
        Assert.Contains("\"nested\"", json);
        Assert.Contains("\"key\":\"value\"", json);
        Assert.Contains("\"list\":[1,2]", json);
    }
}
