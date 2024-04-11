using System.Text;
using Celerio;

namespace CelerioTests;

public class Http11
{
    [Test]
    public void Http11_OK()
    {
        var provider = new HTTP11ProtocolProvider();
        
        Stream stream = new MemoryStream();
        StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);
        
        writer.Write("GET /ggg/fsdf/dfghdfg?a=b&h=c HTTP/1.1\r\nHEADER_TEST: test555\r\nContent-Length: 3\r\n\r\nabc");
        writer.Flush();
        
        stream.Position = 0;
        
        var success = provider.GetRequest(stream, out var request);
        Assert.IsTrue(success);
        
        Assert.That(request.Method, Is.EqualTo("GET"));
        Assert.That(request.URI, Is.EqualTo("/ggg/fsdf/dfghdfg"));
        Assert.That(request.Query, Is.EqualTo(new Dictionary<string, string>(){{"a","b"},{"h","c"}}));
        Assert.That(request.Headers, Is.EqualTo(new Dictionary<string, string>(){{"HEADER_TEST", "test555"},{"Content-Length", "3"}}));
        Assert.That(request.Body, Is.EqualTo("abc"));
        
        Assert.Pass();
    }
    
    [Test]
    public void Http11_WrongMeta_NoRoute()
    {
        var provider = new HTTP11ProtocolProvider();
        
        Stream stream = new MemoryStream();
        StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);
        
        writer.Write("GET HTTP/1.1\r\nHEADER_TEST: test555\r\nContent-Length: 3\r\n\r\nabc");
        writer.Flush();
        
        stream.Position = 0;
        
        var success = provider.GetRequest(stream, out var request);
        Assert.IsFalse(success);
        
        Assert.Pass();
    }
    [Test]
    public void Http11_WrongMeta_NoVersion()
    {
        var provider = new HTTP11ProtocolProvider();
        
        Stream stream = new MemoryStream();
        StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);
        
        writer.Write("GET /test\r\nHEADER_TEST: test555\r\nContent-Length: 3\r\n\r\nabc");
        writer.Flush();
        
        stream.Position = 0;
        
        var success = provider.GetRequest(stream, out var request);
        Assert.IsFalse(success);
        
        Assert.Pass();
    }
    [Test]
    public void Http11_WrongMeta_WrongVersion()
    {
        var provider = new HTTP11ProtocolProvider();
        
        Stream stream = new MemoryStream();
        StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);
        
        writer.Write("GET /test HTTP/2.0\r\nHEADER_TEST: test555\r\nContent-Length: 3\r\n\r\nabc");
        writer.Flush();
        
        stream.Position = 0;
        
        var success = provider.GetRequest(stream, out var request);
        Assert.IsFalse(success);
        
        Assert.Pass();
    }
    [Test]
    public void Http11_WrongMeta_NoMethod()
    {
        var provider = new HTTP11ProtocolProvider();
        
        Stream stream = new MemoryStream();
        StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);
        
        writer.Write("/test HTTP/1.1\r\nHEADER_TEST: test555\r\nContent-Length: 3\r\n\r\nabc");
        writer.Flush();
        
        stream.Position = 0;
        
        var success = provider.GetRequest(stream, out var request);
        Assert.IsFalse(success);
        
        Assert.Pass();
    }
    [Test]
    public void Http11_WrongMeta_NoMeta()
    {
        var provider = new HTTP11ProtocolProvider();
        
        Stream stream = new MemoryStream();
        StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);
        
        writer.Write("HEADER_TEST: test555\r\nContent-Length: 3\r\n\r\nabc");
        writer.Flush();
        
        stream.Position = 0;
        
        var success = provider.GetRequest(stream, out var request);
        Assert.IsFalse(success);
        
        Assert.Pass();
    }
    
    [Test]
    public void Http11_WrongHeaders_NoSplitter()
    {
        var provider = new HTTP11ProtocolProvider();
        
        Stream stream = new MemoryStream();
        StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);
        
        writer.Write("GET / HTTP/1.1\r\nHEADER_TEST=test555\r\nContent-Length: 3\r\n\r\nabc");
        writer.Flush();
        
        stream.Position = 0;
        
        var success = provider.GetRequest(stream, out var request);
        Assert.IsFalse(success);
        
        Assert.Pass();
    }
    [Test]
    public void Http11_WrongBody_LengthNotSpecified()
    {
        var provider = new HTTP11ProtocolProvider();
        
        Stream stream = new MemoryStream();
        StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);
        
        writer.Write("GET / HTTP/1.1\r\nHEADER_TEST: test555\r\n\r\nabc");
        writer.Flush();
        
        stream.Position = 0;
        
        var success = provider.GetRequest(stream, out var request);
        Assert.IsTrue(success);
        Assert.That(request.Body, Is.Null);
        
        Assert.Pass();
    }
    [Test]
    public void Http11_WrongBody_NoBodySplitter()
    {
        var provider = new HTTP11ProtocolProvider();
        
        Stream stream = new MemoryStream();
        StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);
        
        writer.Write("GET / HTTP/1.1\r\nHEADER_TEST: test555\r\nContent-Length: 3\r\nabc");
        writer.Flush();
        
        stream.Position = 0;
        
        var success = provider.GetRequest(stream, out var request);
        Assert.IsFalse(success);
        
        Assert.Pass();
    }
}