using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Celerio;


public interface IAuthentication
{
    public object? Authenticate(HttpRequest request);
    public HttpResponse SendAuthentication(object data);
}

public class Authentication<T> : IAuthentication
{
    private readonly byte[] key;
    public readonly TimeSpan TokenExpiration = TimeSpan.FromDays(24);
    
    public object? Authenticate(HttpRequest request)
    {
        if (!request.Headers.TryGetSingle("Authorization", out var auth))
            return null;
        

        var authParts = auth.Split(' ');

        string t;
        if (authParts.Length == 2)
            t = authParts[1];
        else if (authParts.Length == 1)
            t = auth;
        else
            return null;
        
        var token = AuthToken<T>.Unpack(t, key);

        if (token == null)
            return null;
        
        return token.Data;
    }
    
    public HttpResponse SendAuthentication(object info)
    {
        var token = new AuthToken<T>(DateTime.UtcNow + TokenExpiration, (T)info);
        var pack = token.Pack(key);
        return new HttpResponse(200, "OK", JsonConvert.SerializeObject(new {code = pack, expires = token.Until}));
    }
    
    public Authentication(string key)
    {
        this.key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
    }
}

