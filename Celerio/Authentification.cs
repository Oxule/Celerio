using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Celerio;


public interface IAuthentification
{
    public object? Authentificate(HttpRequest request);
    public HttpResponse SendAuthentification(object data);

    public Type DataType { get; set; }
}

public class DefaultAuthentification : IAuthentification
{
    private readonly byte[] key;
    public TimeSpan TokenExpiration = TimeSpan.FromDays(24);
    public Type DataType { get; set; } = typeof(long);
    
    public object? Authentificate(HttpRequest request)
    {
        var auth = request.Headers["Authorization"];
        if (auth.Count != 1)
            return null;

        var token = AuthToken.Unpack(auth[0], key);

        if (token == null)
            return null;

        if (token.Until <= DateTime.UtcNow)
            return null;
        
        return token.Data;
    }
    
    public HttpResponse SendAuthentification(object info)
    {
        if (info.GetType() != DataType)
            throw new Exception("info's type must be equal to the DataType");
        var token = new AuthToken(DateTime.UtcNow + TokenExpiration, info);
        var pack = token.Pack(key);
        return new HttpResponse(200, "OK", pack);
    }
    
    public DefaultAuthentification(string key)
    {
        this.key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
    }
}

