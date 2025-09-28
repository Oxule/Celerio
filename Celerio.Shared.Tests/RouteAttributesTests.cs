using Celerio;
using Xunit;
using Celerio.Shared;

public class RouteAttributesTests
{
    [Fact]
    public void GetAttribute_SetsPattern()
    {
        var attr = new GetAttribute("/test");
        Assert.Equal("/test", attr.Pattern);
    }

    [Fact]
    public void PostAttribute_SetsPattern()
    {
        var attr = new PostAttribute("/post");
        Assert.Equal("/post", attr.Pattern);
    }

    [Fact]
    public void PutAttribute_SetsPattern()
    {
        var attr = new PutAttribute("/put");
        Assert.Equal("/put", attr.Pattern);
    }

    [Fact]
    public void PatchAttribute_SetsPattern()
    {
        var attr = new PatchAttribute("/patch");
        Assert.Equal("/patch", attr.Pattern);
    }

    [Fact]
    public void DeleteAttribute_SetsPattern()
    {
        var attr = new DeleteAttribute("/del");
        Assert.Equal("/del", attr.Pattern);
    }

    [Fact]
    public void RouteAttribute_SetsMethodAndPattern()
    {
        var attr = new RouteAttribute("OPTIONS", "/opt");
        Assert.Equal("OPTIONS", attr.Method);
        Assert.Equal("/opt", attr.Pattern);
    }
}
