using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Celerio;

public static class JWT
{
    public static string CreateToken(object payload)
    {
        var header = "{\"alg\":\"HS256\",\"typ\":\"JWT\"}";
        var headerEncoded = EncodeBase64Url(Encoding.UTF8.GetBytes(header));

        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadEncoded = EncodeBase64Url(Encoding.UTF8.GetBytes(payloadJson));

        var message = $"{headerEncoded}.{payloadEncoded}";
        var signature = ComputeHMAC(message);
        var signatureEncoded = EncodeBase64Url(signature);

        return $"{headerEncoded}.{payloadEncoded}.{signatureEncoded}";
    }

    public static string? ValidateToken(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
            return null;

        var headerEncoded = parts[0];
        var payloadEncoded = parts[1];
        var signatureEncoded = parts[2];

        var headerJson = DecodeBase64Url(headerEncoded);
        if (headerJson == null || !headerJson.Contains("\"alg\":\"HS256\""))
            return null;

        var message = $"{headerEncoded}.{payloadEncoded}";
        var expectedSignature = ComputeHMAC(message);
        var providedSignature = DecodeBase64UrlBytes(signatureEncoded);
        if (expectedSignature == null || providedSignature == null ||
            !AreEqual(expectedSignature, providedSignature))
            return null;

        var payloadJson = DecodeBase64Url(payloadEncoded);
        return payloadJson;
    }

    private static byte[]? ComputeHMAC(string message)
    {
        using var hmac = new HMACSHA256(Environment.AUTH_SECRET);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
    }

    private static string EncodeBase64Url(byte[] input)
    {
        var base64 = Convert.ToBase64String(input);
        return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static string? DecodeBase64Url(string input)
    {
        try
        {
            var output = input.Replace('-', '+').Replace('_', '/');
            switch (output.Length % 4)
            {
                case 2: output += "=="; break;
                case 3: output += "="; break;
            }
            var bytes = Convert.FromBase64String(output);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return null;
        }
    }

    private static byte[]? DecodeBase64UrlBytes(string input)
    {
        try
        {
            var output = input.Replace('-', '+').Replace('_', '/');
            switch (output.Length % 4)
            {
                case 2: output += "=="; break;
                case 3: output += "="; break;
            }
            return Convert.FromBase64String(output);
        }
        catch
        {
            return null;
        }
    }

    private static bool AreEqual(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
            if (a[i] != b[i]) return false;
        return true;
    }
}
