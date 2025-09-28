using Celerio;
using Xunit;
using Celerio.Shared;

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
}
