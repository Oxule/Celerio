using System.Security.Cryptography;
using System.Text;

namespace Celerio;


public interface IAuthentification
{
    public MethodInvoke.InvokeOverride Authentificate(HttpRequest request);
}

public class DefaultAuthentification : IAuthentification
{
    private const string SECRET_KEY = "ILOVE1337BEER555ZXC10007MULMULADDDIV54228";
    private const string SECRET_SALT = "SDFLKUSHJDFLKSJNV2348712693jkxzbdlfkjsdf";
    private readonly Aes Aes;
    public TimeSpan TokenExpiration = TimeSpan.FromDays(24);
    
    public MethodInvoke.InvokeOverride Authentificate(HttpRequest request)
    {
        var a = Auth(request);
        if (a == null || !a.TryGetValue("exp", out var expires) || !DateTime.TryParse(expires, out var exp) ||
            exp <= DateTime.Now)
            a = null;
        return new MethodInvoke.InvokeOverride(typeof(Dictionary<string, string>), a, "auth");
    }

    private Dictionary<string, string>? Auth(HttpRequest request)
    {
        if (!request.Headers.TryGetValue("Authorization", out var auth))
            return null;
        var p = auth.Split(' ');
        if (p.Length != 2)
            return null;
        if (p[0].ToLower() != "bearer")
            return null;
        if (p[1] == "")
            return null;
        
        var lines = Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(p[1]))).Split('\n');
        Dictionary<string, string> cred = new Dictionary<string, string>();
        foreach (var line in lines)
        {
            p = line.Trim().Split(':');
            if(p.Length!=2)
                continue;
            cred.Add(p[0].ToLower(), p[1]);
        }

        return cred;
    }

    public string GenerateToken(Dictionary<string, string> credentials)
    {
        credentials.Add("exp", DateTime.Now.Add(TokenExpiration).ToString("g"));
        StringBuilder sb = new StringBuilder();
        foreach (var kvp in credentials)
        {
            sb.AppendLine($"{kvp.Key}:{kvp.Value}");
        }
        var token = Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(sb.ToString())));
        return token;
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
    
    public DefaultAuthentification()
    {
        var key = MD5.HashData(Encoding.UTF8.GetBytes(SECRET_KEY));
        var salt = MD5.HashData(Encoding.UTF8.GetBytes(SECRET_SALT));       
        var aes = Aes.Create();
        aes.KeySize = 128;
        aes.Key = key;
        aes.IV = salt;
        aes.Mode = CipherMode.CBC;
        Aes = aes;
    }
}

