namespace Celerio;

/// <summary>
/// Represents a collection of HTTP headers with support for multiple header values per name.
/// This implementation uses case-insensitive header name matching and provides efficient
/// storage, retrieval, and manipulation of header data.
/// </summary>
public class HeaderCollection : IEnumerable<KeyValuePair<string,string>>
{
    private readonly List<(string Name, string Value)> _entries = new();
    private readonly Dictionary<string, List<int>> _index =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the total number of header entries currently stored in the collection.
    /// Note that this count may include removed entries until Compact() is called.
    /// </summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Adds a new header entry with the specified name and value to the collection.
    /// Multiple headers with the same name are supported, maintaining the order of addition.
    /// </summary>
    /// <param name="name">The name of the header; must not be null.</param>
    /// <param name="value">The value associated with the header.</param>
    /// <exception cref="ArgumentNullException">Thrown if name is null.</exception>
    public void Add(string name, string value)
    {
        if (name == null) throw new ArgumentNullException(nameof(name));
        var idx = _entries.Count;
        _entries.Add((name, value));
        if (!_index.TryGetValue(name, out var list))
        {
            list = new List<int>();
            _index[name] = list;
        }
        list.Add(idx);
    }

    /// <summary>
    /// Sets the value for the specified header name by first removing any existing entries
    /// and then adding the new value. This ensures only one value is associated with the name.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <param name="value">The value to be set for the header.</param>
    public void Set(string name, string value)
    {
        Remove(name);
        Add(name, value);
    }

    /// <summary>
    /// Removes all header entries with the specified name from the collection.
    /// </summary>
    /// <param name="name">The header name to remove.</param>
    /// <returns>True if any entries were removed; otherwise, false.</returns>
    public bool Remove(string name)
    {
        if (!_index.TryGetValue(name, out var indices)) return false;
        foreach (var i in indices) _entries[i] = (null!, null!);
        _index.Remove(name);
        return true;
    }

    /// <summary>
    /// Attempts to retrieve all values associated with the specified header name.
    /// </summary>
    /// <param name="name">The header name to retrieve values for (case-insensitive).</param>
    /// <param name="values">When this method returns true, contains the list of header values; otherwise, an empty list.</param>
    /// <returns>True if the header exists and values were retrieved; otherwise, false.</returns>
    public bool TryGetValues(string name, out IReadOnlyList<string> values)
    {
        values = Array.Empty<string>();
        if (!_index.TryGetValue(name, out var indices)) return false;
        var list = new List<string>(indices.Count);
        foreach (var i in indices)
        {
            var e = _entries[i];
            if (e.Name != null) list.Add(e.Value);
        }
        values = list;
        return true;
    }

    /// <summary>
    /// Gets the last (most recently added) value for the specified header name.
    /// If multiple values exist, returns the most recent one.
    /// </summary>
    /// <param name="name">The header name to retrieve the value for (case-insensitive).</param>
    /// <returns>The header value, or null if the header is not found.</returns>
    public string? Get(string name)
    {
        if (!_index.TryGetValue(name, out var indices)) return null;
        for (int k = indices.Count - 1; k >= 0; k--)
        {
            var e = _entries[indices[k]];
            if (e.Name != null) return e.Value;
        }
        return null;
    }

    /// <summary>
    /// Compacts the internal storage by removing all inactive (previously removed) entries.
    /// This operation reclaims memory and optimizes future lookups.
    /// Call this method periodically or after many Remove operations.
    /// </summary>
    public void Compact()
    {
        var newEntries = new List<(string, string)>(_entries.Count);
        _index.Clear();
        for (int i = 0; i < _entries.Count; i++)
        {
            var e = _entries[i];
            if (e.Name == null) continue;
            var idx = newEntries.Count;
            newEntries.Add(e);
            if (!_index.TryGetValue(e.Name, out var list))
            {
                list = new List<int>();
                _index[e.Name] = list;
            }
            list.Add(idx);
        }
        _entries.Clear();
        _entries.AddRange(newEntries);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the active header entries in the collection.
    /// </summary>
    /// <returns>An enumerator for KeyValuePair<string, string> representing header names and values.</returns>
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        foreach (var e in _entries)
            if (e.Name != null)
                yield return new KeyValuePair<string, string>(e.Name, e.Value);
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Asynchronously writes all active header entries to the specified StreamWriter in HTTP header format.
    /// Each header is written as "Name: Value" followed by a newline.
    /// </summary>
    /// <param name="writer">The StreamWriter to write the headers to.</param>
    /// <returns>A Task representing the asynchronous write operation.</returns>
    public async Task WriteHeadersAsync(StreamWriter writer)
    {
        foreach (var e in _entries)
            if (e.Name != null)
                await writer.WriteLineAsync($"{e.Name}: {e.Value}");
    }
}
