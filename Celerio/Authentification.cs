﻿using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Celerio;


public interface IAuthentification
{
    public dynamic? Authentificate(HttpRequest request);
    public HttpResponse SendAuthentification(object data);
}

public class DefaultAuthentification : IAuthentification
{
    private readonly byte[] key;
    public TimeSpan TokenExpiration = TimeSpan.FromDays(24);
    
    public dynamic? Authentificate(HttpRequest request)
    {
        var auth = request.Headers["Authorization"];
        if (auth.Count != 1)
            return null;

        var authParts = auth[0].Split(' ');

        string t;
        if (authParts.Length == 2)
            t = authParts[1];
        else if (authParts.Length == 1)
            t = auth[0];
        else
            return null;
        
        var token = AuthToken.Unpack(t, key);

        if (token == null)
            return null;
        
        return token.Data;
    }
    
    public HttpResponse SendAuthentification(object info)
    {
        var token = new AuthToken(DateTime.UtcNow + TokenExpiration, info);
        var pack = token.Pack(key);
        return new HttpResponse(200, "OK", JsonConvert.SerializeObject(new {code = pack, expires = token.Until}));
    }
    
    public DefaultAuthentification(string key)
    {
        this.key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
    }
}

