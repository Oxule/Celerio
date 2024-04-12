using Celerio;

namespace CelerioTests;

public class PathMatching
{
    [Test]
    public void Match_OK_NoParams_Root()
    {
        PathMatcher matcher = new PathMatcher();
        
        Assert.IsTrue(matcher.Match("/", "/", out var parameters));
    }
    
    [Test]
    public void Match_OK_NoParams_Directory()
    {
        PathMatcher matcher = new PathMatcher();
        
        Assert.IsTrue(matcher.Match("/test", "/test", out var parameters));
    }
    
    [Test]
    public void Match_OK_NoParams_DeepPath()
    {
        PathMatcher matcher = new PathMatcher();
        
        Assert.IsTrue(matcher.Match("/test/test2/555", "/test/test2/555", out var parameters));
    }
    
    [Test]
    public void Match_OK_Params_1()
    {
        PathMatcher matcher = new PathMatcher();
        
        Assert.IsTrue(matcher.Match("/params/321", "/params/{test}", out var parameters));
        
        Assert.That(parameters["test"], Is.EqualTo("321"));
    }
    [Test]
    public void Match_OK_Params_3()
    {
        PathMatcher matcher = new PathMatcher();
        
        Assert.IsTrue(matcher.Match("/params/321/page/4/test", "/params/{test}/page/{page}/{a}", out var parameters));
        
        Assert.That(parameters["test"], Is.EqualTo("321"));
        Assert.That(parameters["page"], Is.EqualTo("4"));
        Assert.That(parameters["a"], Is.EqualTo("test"));

    }
    
    [Test]
    public void Match_OK_NoClosingBackslash()
    {
        PathMatcher matcher = new PathMatcher();
        
        Assert.IsTrue(matcher.Match("/Test", "/Test", out var parameters1));
        Assert.IsTrue(matcher.Match("/Test/", "/Test", out var parameters2));
        Assert.IsTrue(matcher.Match("/Test", "/Test/", out var parameters3));
        Assert.IsTrue(matcher.Match("/Test/", "/Test/", out var parameters4));

    }
    
    [Test]
    public void Match_Wrong_NoOpeningBackslash()
    {
        PathMatcher matcher = new PathMatcher();
        
        Assert.IsFalse(matcher.Match("Test", "/Test", out var parameters1));
        Assert.IsFalse(matcher.Match("/Test", "Test", out var parameters2));
        Assert.IsFalse(matcher.Match("Test", "Test", out var parameters3));
    }
    
    [Test]
    public void Match_Wrong()
    {
        PathMatcher matcher = new PathMatcher();
        
        Assert.IsFalse(matcher.Match("/api/", "/test", out var parameters1));
        
        Assert.IsFalse(matcher.Match("/", "/test", out var parameters2));
        
        Assert.IsFalse(matcher.Match("/ggg/", "/", out var parameters3));
        
        Assert.IsFalse(matcher.Match("/ggg/page", "/ggg/", out var parameters4));
        
        Assert.IsFalse(matcher.Match("/api/", "/test/{test}", out var parameters5));
        
        Assert.IsFalse(matcher.Match("/", "/{ggg}", out var parameters6));
        
        Assert.IsFalse(matcher.Match("/ggg/", "/{}/gdfg", out var parameters7));
    }
}