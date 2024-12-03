using System.Collections;

namespace Celerio;

public class HeadersCollection : IEnumerable<KeyValuePair<string, List<string>>>
{
    private Dictionary<string, List<string>> _headers = new ();

    public List<string> this[string key]
    {
        get
        {
            if (_headers.TryGetValue(key, out var value))
                return value;
            return new List<string>();
        }
        set
        {
            if (!_headers.ContainsKey(key))
                _headers[key] = new List<string>();
            _headers[key] = value;
        }
    }
    
    public void Add(string key, string value)
    {
        if (_headers.TryGetValue(key, out var v))
        {
            v.Add(value);
        }
        else
        {
            _headers.Add(key, new (){value});
        }
    }

    public bool TryGet(string key, out List<string> value) => _headers.TryGetValue(key, out value);
    public bool Contains(string key) => _headers.ContainsKey(key);
    
    public IEnumerator<KeyValuePair<string, List<string>>> GetEnumerator()
    {
        return _headers.GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}