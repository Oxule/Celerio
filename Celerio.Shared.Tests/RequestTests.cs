using Celerio;
using Xunit;
using Celerio.Shared;

public class RequestTests
{
    [Fact]
    public void Constructor_SetsFields()
    {
        var query = new Dictionary<string, string> { ["key"] = "value" };
        var headers = new HeaderCollection();
        var body = new byte[] { 1, 2, 3 };
        var request = new Request("GET", "/path", query, headers, body);

        Assert.Equal("GET", request.Method);
        Assert.Equal("/path", request.Path);
        Assert.Equal(query, request.Query);
        Assert.Equal(headers, request.Headers);
        Assert.Equal(body, request.Body);
    }
    
    [Fact]
    public void Constructor_QueryWithMultipleParameters()
    {
        var query = new Dictionary<string, string> { ["key1"] = "value1", ["key2"] = "value2" };
        var request = new Request("GET", "/path", query, new(), Array.Empty<byte>());
        Assert.Equal("value1", request.Query["key1"]);
        Assert.Equal("value2", request.Query["key2"]);
    }
}
