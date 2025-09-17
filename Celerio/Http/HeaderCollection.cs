namespace Celerio;

public class HeaderCollection : IEnumerable<KeyValuePair<string,string>>
{
    private readonly List<(string Name, string Value)> _entries = new();
    private readonly Dictionary<string, List<int>> _index =
        new(StringComparer.OrdinalIgnoreCase);

    public int Count => _entries.Count;

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

    public void Set(string name, string value)
    {
        Remove(name);
        Add(name, value);
    }

    public bool Remove(string name)
    {
        if (!_index.TryGetValue(name, out var indices)) return false;
        foreach (var i in indices) _entries[i] = (null!, null!);
        _index.Remove(name);
        return true;
    }

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

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        foreach (var e in _entries)
            if (e.Name != null)
                yield return new KeyValuePair<string, string>(e.Name, e.Value);
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    public async Task WriteHeadersAsync(StreamWriter writer)
    {
        foreach (var e in _entries)
            if (e.Name != null)
                await writer.WriteLineAsync($"{e.Name}: {e.Value}");
    }
}
