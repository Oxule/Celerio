using Xunit;
using Celerio.Shared;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Celerio;

public class HeaderCollectionTests
{
    [Fact]
    public void Add_ValidHeaders_IncreasesCount()
    {
        var headers = new HeaderCollection();
        headers.Add("Content-Type", "application/json");
        headers.Add("Accept", "text/html");

        Assert.Equal(2, headers.Count);
    }

    [Fact]
    public void Remove_NullName_ThrowsArgumentNullException()
    {
        var headers = new HeaderCollection();
        Assert.Throws<ArgumentNullException>(() => headers.Remove(null));
    }

    [Fact]
    public void Set_OverwritesExisting()
    {
        var headers = new HeaderCollection();
        headers.Add("Test", "first");
        headers.Add("Test", "second");
        Assert.Equal("second", headers.Get("Test"));
        Assert.Equal(2, headers.Count);
        headers.Set("Test", "replaced");
        Assert.Equal(3, headers.Count);
        Assert.Equal("replaced", headers.Get("Test"));
        headers.Add("Test", "first");
        headers.Add("Test", "second");
        Assert.Equal(5, headers.Count);
    }

    [Fact]
    public void Remove_ExistingHeader_ReturnsTrueAndRemoves()
    {
        var headers = new HeaderCollection();
        headers.Add("Test", "value");
        var removed = headers.Remove("Test");
        Assert.True(removed);
        Assert.Null(headers.Get("Test"));
        Assert.Equal(1, headers.Count);
    }

    [Fact]
    public void TryGetValues_Existing_ReturnsTrueAndValues()
    {
        var headers = new HeaderCollection();
        headers.Add("Test", "first");
        headers.Add("Test", "second");

        var success = headers.TryGetValues("Test", out var values);
        Assert.True(success);
        Assert.Equal(2, values.Count);
        Assert.Contains("first", values);
        Assert.Contains("second", values);
        
        headers.Set("Test", "replaced");
        
        success = headers.TryGetValues("Test", out var valuesReplaced);
        Assert.True(success);
        Assert.Single(valuesReplaced);
        Assert.Equal("replaced", valuesReplaced[0]);
    }

    [Fact]
    public void TryGetValues_NonExisting_ReturnsFalse()
    {
        var headers = new HeaderCollection();
        var success = headers.TryGetValues("Test", out var values);
        Assert.Equal(0, headers.Count);
        Assert.False(success);
        Assert.Empty(values);
    }

    [Fact]
    public void Get_Existing_ReturnsLast()
    {
        var headers = new HeaderCollection();
        headers.Add("Test", "first");
        headers.Add("Test", "second");
        Assert.Equal("second", headers.Get("Test"));
        Assert.Equal(2, headers.Count);
    }

    [Fact]
    public void Get_NonExisting_ReturnsNull()
    {
        var headers = new HeaderCollection();
        Assert.Null(headers.Get("Test"));
    }

    [Fact]
    public void Compact_RemovesInactiveEntries()
    {
        var headers = new HeaderCollection();
        headers.Add("A", "a");
        headers.Add("B", "b");
        headers.Remove("A");
        Assert.Equal(2, headers.Count);
        headers.Compact();
        Assert.Equal(1, headers.Count);
        Assert.Equal("b", headers.Get("B"));
    }

    [Fact]
    public void Enumerator_IncludesActiveHeaders()
    {
        var headers = new HeaderCollection();
        headers.Add("A", "a");
        headers.Add("B", "b");
        headers.Remove("A");

        var list = headers.ToList();
        Assert.Single(list);
        Assert.Equal(new KeyValuePair<string, string>("B", "b"), list[0]);
    }

    [Fact]
    public void CaseInsensitive_NameMatching()
    {
        var headers = new HeaderCollection();
        headers.Add("content-type", "json");
        Assert.Equal("json", headers.Get("Content-Type"));
        Assert.Equal("json", headers.Get("CONTENT-TYPE"));
    }

    [Fact]
    public void EmptyCollection_EnumerationEmpty()
    {
        var headers = new HeaderCollection();
        Assert.Empty(headers.ToList());
    }

    [Fact]
    public void AddAfterRemove_GetsNewValue()
    {
        var headers = new HeaderCollection();
        headers.Add("Test", "old");
        headers.Remove("Test");
        headers.Add("Test", "new");
        Assert.Equal("new", headers.Get("Test"));
    }

    [Fact]
    public void CompactMultipleTimes_NoErrors()
    {
        var headers = new HeaderCollection();
        headers.Compact();
        headers.Compact();
        Assert.Equal(0, headers.Count);
    }

    [Fact]
    public async Task WriteHeadersAsync_AddsLines()
    {
        var headers = new HeaderCollection();
        headers.Add("A", "1");
        headers.Add("B", "2");

        using var ms = new MemoryStream();
        using var sw = new StreamWriter(ms);
        await headers.WriteHeadersAsync(sw);
        await sw.FlushAsync();
        var output = Encoding.ASCII.GetString(ms.ToArray());
        Assert.Contains("A: 1\r\n", output);
        Assert.Contains("B: 2\r\n", output);
    }

    [Fact]
    public async Task WriteHeadersAsync_EmptyCollection_WritesNothing()
    {
        var headers = new HeaderCollection();

        using var ms = new MemoryStream();
        using var sw = new StreamWriter(ms);
        await headers.WriteHeadersAsync(sw);
        await sw.FlushAsync();
        var output = Encoding.ASCII.GetString(ms.ToArray());
        Assert.Equal("", output);
    }

    [Fact]
    public void Add_EmptyValue_IsAccepted()
    {
        var headers = new HeaderCollection();
        headers.Add("Empty", "");
        Assert.Equal("", headers.Get("Empty"));
    }

    [Fact]
    public void Set_NullName_ThrowsArgumentNullException()
    {
        var headers = new HeaderCollection();
        Assert.Throws<ArgumentNullException>(() => headers.Set(null, "value"));
    }

    [Fact]
    public void Compact_OnEmptyList_NoChange()
    {
        var headers = new HeaderCollection();
        headers.Compact();
        Assert.Equal(0, headers.Count);
    }

    [Fact]
    public void TryGetValues_EmptyValue_Included()
    {
        var headers = new HeaderCollection();
        headers.Add("Test", "");
        var success = headers.TryGetValues("Test", out var values);
        Assert.True(success);
        Assert.Single(values);
        Assert.Equal("", values[0]);
    }
}
