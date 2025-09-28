using Xunit;
using Celerio.Shared;
using System.Text.Json;
using System.Text;
using Celerio;

public class JWTTests
{
    [Fact]
    public void CreateToken_ValidPayload_CreatesToken()
    {
        var payload = new { foo = "bar", num = 42 };
        var token = JWT.CreateToken(payload);

        var parts = token.Split('.');
        Assert.Equal(3, parts.Length);

        var headerJson = DecodeBase64Url(parts[0]);
        Assert.Contains("\"alg\":\"HS256\"", headerJson);
        Assert.Contains("\"typ\":\"JWT\"", headerJson);

        var payloadJson = DecodeBase64Url(parts[1]);
        var receivedPayload = JsonSerializer.Deserialize<Dictionary<string, object>>(payloadJson);
        Assert.NotNull(receivedPayload);
        Assert.Equal("bar", receivedPayload["foo"].ToString());
        Assert.Equal(42, ((JsonElement)receivedPayload["num"]).GetInt32());
    }

    [Fact]
    public void CreateToken_NullPayload_ReturnsToken()
    {
        var token = JWT.CreateToken(null);
        Assert.NotNull(token);
        var result = JWT.ValidateToken(token);
        Assert.Equal("null", result);
    }

    [Fact]
    public void CreateToken_ComplexPayload_SerializesCorrectly()
    {
        var payload = new { nested = new { deep = true }, array = new[] { 1, 2, 3 } };
        var token = JWT.CreateToken(payload);
        var parts = token.Split('.');
        var payloadJson = DecodeBase64Url(parts[1]);
        var received = JsonSerializer.Deserialize<Dictionary<string, object>>(payloadJson);
        Assert.NotNull(received);
    }

    [Fact]
    public void ValidateToken_ValidToken_ReturnsPayload()
    {
        var payload = new { test = "value" };
        var token = JWT.CreateToken(payload);

        var result = JWT.ValidateToken(token);
        Assert.NotNull(result);

        var received = JsonSerializer.Deserialize<Dictionary<string, object>>(result);
        Assert.Equal("value", received["test"].ToString());
    }

    [Fact]
    public void ValidateToken_InvalidParts_ReturnsNull()
    {
        Assert.Null(JWT.ValidateToken("invalid"));
        Assert.Null(JWT.ValidateToken("a.b"));
        Assert.Null(JWT.ValidateToken("a.b.c.d"));
    }

    [Fact]
    public void ValidateToken_InvalidHeaderAlg_ReturnsNull()
    {
        var token = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJ0ZXN0IjoidmFsdWUifQ.signature"; // Alg RS256
        Assert.Null(JWT.ValidateToken(token));
    }

    [Fact]
    public void ValidateToken_InvalidHeaderJson_ReturnsNull()
    {
        var invalidHeader = "invalidbase64";
        var token = invalidHeader + ".eyJ0ZXN0IjoidmFsdWUifQ.signature"; // Invalid base64
        Assert.Null(JWT.ValidateToken(token));
    }

    [Fact]
    public void ValidateToken_TamperSignature_ReturnsNull()
    {
        var payload = new { test = "value" };
        var token = JWT.CreateToken(payload);

        var parts = token.Split('.');
        parts[2] = "invalid";
        var tampered = string.Join('.', parts);
        Assert.Null(JWT.ValidateToken(tampered));
    }

    [Fact]
    public void ValidateToken_WrongSignature_ReturnsNull()
    {
        var fakeToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ0ZXN0IjoidmFsdWUifQ.fake";
        Assert.Null(JWT.ValidateToken(fakeToken));
    }

    [Fact]
    public void ValidateToken_EmptyToken_ReturnsNull()
    {
        Assert.Null(JWT.ValidateToken(""));
    }
    
    [Fact]
    public void ValidateToken_NullToken_Exception()
    {
        Assert.Throws<NullReferenceException>(()=>JWT.ValidateToken(null));
    }

    [Fact]
    public void ValidateToken_MultipleDotsInParts_ReturnsNull()
    {
        var token = "header.pay.load.signature"; // 4 parts, but should be 3 with dots
        var parts = token.Split('.');
        Assert.Equal(4, parts.Length);
        Assert.Null(JWT.ValidateToken(token));
    }

    [Fact]
    public void ValidateToken_InvalidAlgorithm_None_ReturnsNull()
    {
        // alg "none"
        var token = "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.eyJ0ZXN0IjoidmFsdWUifQ."; // signature empty
        Assert.Null(JWT.ValidateToken(token));
    }

    private string DecodeBase64Url(string input)
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
}
