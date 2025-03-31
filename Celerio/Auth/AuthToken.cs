using System.Security.Cryptography;
using System.Text;
using SpanJson;
using SpanJson.Resolvers;

namespace Celerio;

public class AuthToken<T>
{
    public DateTime Until = DateTime.MaxValue;
    public T? Data = default;

    public string Pack(byte[] key)
    {
        var obj = JsonSerializer.NonGeneric.Utf8.Serialize<ExcludeNullsCamelCaseResolver<byte>>(this);
        
        HMACSHA256 hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(obj);

        const string header = "{\n  \"alg\": \"HS256\",\n  \"typ\": \"JWT\"\n}";

        var h = Encoding.UTF8.GetBytes(header);
        
        return Convert.ToBase64String(h)+"."+Convert.ToBase64String(obj)+"."+Convert.ToBase64String(hash);
    }

    public static AuthToken<T>? Unpack(string token, byte[] key)
    {
        var firstSplit = token.IndexOf('.');
        if (firstSplit == -1)
            return null;
        var secondSplit = token.IndexOf('.', firstSplit + 1);
        if (secondSplit == -1)
            return null;
        var chars = token.ToCharArray();
        
        var body = Convert.FromBase64CharArray(chars,firstSplit+1, secondSplit - firstSplit-1);
        
        HMACSHA256 hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(body);

        var checksum = Convert.FromBase64CharArray(chars, secondSplit+1, token.Length-secondSplit-1);
        
        if (!hash.SequenceEqual(checksum))
            return null;
        
        try
        {
            var obj = JsonSerializer.Generic.Utf8.Deserialize<AuthToken<T>,ExcludeNullsCamelCaseResolver<byte>>(body);
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

    [JsonConstructor]
    public AuthToken(DateTime until, T data)
    {
        Until = until;
        Data = data;
    }

    public AuthToken() { }
    
    private static string ToHex(byte[] bytes)
    {
        StringBuilder sb = new StringBuilder(bytes.Length * 3);
        foreach (var b in bytes)
            sb.Append(b.ToString("X2") + ' ');
        return sb.ToString();
    }
}