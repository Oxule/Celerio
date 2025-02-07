using System.Collections;

namespace Celerio;

public class HeadersCollection : IEnumerable<KeyValuePair<string, List<string>>>
{
    private readonly Dictionary<string, List<string>> _headers;

    public HeadersCollection()
    {
        _headers = new (StringComparer.OrdinalIgnoreCase);
    }
    
    public void Set(string key, string value)
    {
        _headers[key] = new List<string> { value };
    }
    
    public void Set(string key, List<string> values)
    {
        _headers[key] = values;
    }
    
    public void Add(string key, string value)
    {
        if (_headers.TryGetValue(key, out var values))
        {
            values.Add(value);
        }
        else
        {
            _headers[key] = new List<string> { value };
        }
    }

    public List<string> Get(string key) => _headers.TryGetValue(key, out List<string> val) ? val : new ();
    
    public string? GetFirst(string key) => _headers.TryGetValue(key, out List<string> val) ? val.FirstOrDefault() : null;

    public string? GetSingle(string key)
    {
        if (_headers.TryGetValue(key, out List<string> val))
        {
            if (val.Count != 1)
                return null;
            return val.FirstOrDefault();
        }

        return null;
    }

    
    public bool TryGet(string key, out List<string> value) => _headers.TryGetValue(key, out value);

    public bool TryGet(string key, out string? value)
    {
        if (_headers.TryGetValue(key, out var v))
        {
            value = v.FirstOrDefault();
            return value != null;
        }

        value = null;
        return false;
    }
    public bool TryGetSingle(string key, out string? value)
    {
        if (_headers.TryGetValue(key, out var v))
        {
            if (v.Count != 1)
            {
                value = null;
                return false;
            }

            value = v.FirstOrDefault();
            return value != null;
        }

        value = null;
        return false;
    }

    
    public bool Contains(string key) => _headers.ContainsKey(key);

    public IEnumerator<KeyValuePair<string, List<string>>> GetEnumerator() => _headers.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}