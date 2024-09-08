using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Celerio;

public class AuthToken
{
    public DateTime Until = DateTime.MaxValue;
    public object Data = null;

    public string Pack(byte[] key)
    {
        var obj = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this));
        
        HMACSHA1 hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(obj);
        
        byte[] tokenBuffer = new byte[obj.Length + 20];
        Array.Copy(hash, tokenBuffer, 20);
        Array.Copy(obj, 0, tokenBuffer, 20, obj.Length);
        
        var aes = Aes.Create();
        aes.Key = SHA256.HashData(key);
        aes.GenerateIV();
        var iv = aes.IV;
        
        var tokenEncrypt = aes.CreateEncryptor().TransformFinalBlock(tokenBuffer, 0, tokenBuffer.Length);
        
        byte[] ivBuffer = new byte[tokenEncrypt.Length + 16];
        Array.Copy(iv, ivBuffer, 16);
        Array.Copy(tokenEncrypt, 0, ivBuffer, 16, tokenEncrypt.Length);
        
        return Convert.ToBase64String(ivBuffer);
    }

    public static AuthToken? Unpack(string token, byte[] key)
    {
        var tokenEncrypt = Convert.FromBase64String(token);
        
        var aes = Aes.Create();
        aes.Key = SHA256.HashData(key);
        var ivBuffer = new byte[16];
        Array.Copy(tokenEncrypt, ivBuffer, 16);
        aes.IV = ivBuffer;
        
        var tokenBuffer = aes.CreateDecryptor().TransformFinalBlock(tokenEncrypt, 16, tokenEncrypt.Length-16);
        if (tokenBuffer.Length <= 20)
            return null;
        
        HMACSHA1 hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(tokenBuffer, 20, tokenBuffer.Length-20);
        
        if (!CompareArrays(hash,tokenBuffer, 20))
            return null;
        
        try
        {
            var obj = JsonConvert.DeserializeObject<AuthToken>(Encoding.UTF8.GetString(tokenBuffer, 20,
                tokenBuffer.Length - 20));
            if (obj == null)
                return null;
            if (obj.Until <= DateTime.UtcNow)
                return null;
            return obj;
        }
        catch (Exception e)
        {
            return null;
        }
    }

    public AuthToken(DateTime until, object data)
    {
        Until = until;
        Data = data;
    }

    public AuthToken() { }

    private static bool CompareArrays(byte[] a, byte[] b, int length)
    {
        if (a.Length < length || b.Length < length)
            return false;

        for (int i = 0; i < length; i++)
        {
            if (a[i] != b[i])
                return false;
        }
        
        return true;
    }

    private static string ToHex(byte[] bytes)
    {
        StringBuilder sb = new StringBuilder(bytes.Length * 3);
        foreach (var b in bytes)
            sb.Append(b.ToString("X2") + ' ');
        return sb.ToString();
    }
}