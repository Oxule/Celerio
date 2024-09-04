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
    private byte[][] _keys;
    private const int KEYS_COUNT = 5;
    private const int KEY_SIZE = 32;
    public TimeSpan TokenExpiration = TimeSpan.FromDays(24);
    public Type DataType { get; set; } = typeof(long);

    public object? Authentificate(HttpRequest request)
    {
        var auth = request.GetCookie("auth");
        if (auth == null)
            return null;
        var code = Convert.FromBase64String(auth);
        if (code.Length <= 72)
            return null;
        var hash = new byte[64];
        Array.Copy(code, hash, 64);
        var exp = DateTime.FromBinary(BitConverter.ToInt64(code, 64));
        if (exp <= DateTime.UtcNow)
            return null;
        var data = new byte[code.Length - 72];
        Array.Copy(code, 72, data, 0, data.Length);
        byte[] preHash = new byte[8 + data.Length];
        Array.Copy(code, 64, preHash, 0, data.Length+8);
        var correctHash = Encrypt(SHA512.HashData(preHash));
        if (!correctHash.SequenceEqual(hash))
            return null;

        return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(data), DataType);
    }

    //[HASH(64){[EXPIARY(8)],[DATA]}]
    
    public HttpResponse SendAuthentification(object info)
    {
        if (info.GetType() != DataType)
            throw new Exception("info's type must be equal to the DataType");
        var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(info));
        byte[] preHash = new byte[8 + data.Length];
        var exp = BitConverter.GetBytes((DateTime.UtcNow+TokenExpiration).ToBinary());
        Array.Copy(exp, 0, preHash, 0, 8);
        Array.Copy(data, 0, preHash, 8, data.Length);
        var hash = Encrypt(SHA512.HashData(preHash));
        byte[] code = new byte[72 + data.Length];
        Array.Copy(hash, code, 64);
        Array.Copy(preHash, 0, code, 64, preHash.Length);
        var token = Convert.ToBase64String(code);
        return new HttpResponse(200, "OK").AddHeader("Set-Cookie", $"auth={token}; HttpOnly; Max-Age={(long)TokenExpiration.TotalSeconds}; Path=/; Secure");
    }
    
    public byte[] Encrypt(byte[] array)
    {
        for (int i = 0; i < KEYS_COUNT; i++)
        {
            for (int j = 0; j < array.Length; j++)
            {
                array[j] ^= _keys[i][j % KEY_SIZE];
            }
        }

        return array;
    }
    
    public byte[] Decrypt(byte[] array)
    {
        for (int i = KEYS_COUNT-1; i >= 0; i--)
        {
            for (int j = 0; j < array.Length; j++)
            {
                array[j] ^= _keys[i][j % KEY_SIZE];
            }
        }

        return array;
    }
    
    public DefaultAuthentification(string key)
    {
        _keys = new byte[KEY_SIZE][];
        _keys[0] = SHA512.HashData(Encoding.UTF8.GetBytes(key));
        for (int i = 1; i < KEY_SIZE; i++)
        {
            _keys[i] = SHA512.HashData(_keys[i - 1]);
        }
    }
}

