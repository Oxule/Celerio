using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Celerio;


public interface IAuthentification
{
    public Dictionary<string, string>? Authentificate(HttpRequest request);
    public HttpResponse SendAuthentification(Dictionary<string, string> claims);
}

public class DefaultAuthentification : IAuthentification
{
    private readonly Aes Aes;
    public TimeSpan TokenExpiration = TimeSpan.FromDays(24);
    
    public Dictionary<string, string>? Authentificate(HttpRequest request)
    {
        var a = Auth(request);
        if (a != null && a.TryGetValue("exp", out var expires) && DateTime.TryParseExact(expires, "yyyy-MM-dd HH-mm", null, DateTimeStyles.None, out var exp) && exp > DateTime.Now)
            return a;
        
        return null;
    }

    private Dictionary<string, string>? Auth(HttpRequest request)
    {
        var auth = request.GetCookie("auth");
        if (auth == null)
            return null;
        
        var lines = Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(auth))).Split('\n');
        Dictionary<string, string> cred = new Dictionary<string, string>();
        foreach (var line in lines)
        {
            var p = line.Trim().Split(':');
            if(p.Length!=2)
                continue;
            cred.Add(p[0].ToLower(), p[1]);
        }

        return cred;
    }

    public HttpResponse SendAuthentification(Dictionary<string, string> claims)
    {
        claims.Add("exp", DateTime.Now.Add(TokenExpiration).ToString("yyyy-MM-dd HH-mm"));
        StringBuilder sb = new StringBuilder();
        foreach (var kvp in claims)
        {
            sb.AppendLine($"{kvp.Key}:{kvp.Value}");
        }
        var token = Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(sb.ToString())));
        return new HttpResponse(200, "OK", new Dictionary<string, string>() {{"Set-Cookie", $"auth={token}"}}, "");
    }

    public byte[] Encrypt(byte[] data)
    {
        var enc = Aes.CreateEncryptor();
        var ms = new MemoryStream();
        var cs = new CryptoStream(ms, enc, CryptoStreamMode.Write);
        cs.Write(data, 0, data.Length);
        cs.FlushFinalBlock();
        return ms.ToArray();
    }
    
    public byte[] Decrypt(byte[] data)
    {
        var enc = Aes.CreateDecryptor();
        var ms = new MemoryStream();
        var cs = new CryptoStream(ms, enc, CryptoStreamMode.Write);
        cs.Write(data, 0, data.Length);
        cs.FlushFinalBlock();
        return ms.ToArray();
    }
    
    public DefaultAuthentification(string key, string salt)
    {
        var k = MD5.HashData(Encoding.UTF8.GetBytes(key));
        var s = MD5.HashData(Encoding.UTF8.GetBytes(salt));       
        var aes = Aes.Create();
        aes.KeySize = 128;
        aes.Key = k;
        aes.IV = s;
        aes.Mode = CipherMode.CBC;
        Aes = aes;
    }
}

