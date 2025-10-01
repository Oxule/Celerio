using System.Security.Cryptography;

namespace CelerioSamples;

using Celerio;
using static Celerio.Result;

public class Credentials
{
    public string Username { get; set; }
    public int Id { get; set; }
}

public static class Authorization
{
    [Get("/auth")]
    public static Result Auth(Credentials? auth)
    {
        if (auth == null)
        {
            return Unauthorized().Text("I don't know who you are :(\nPass your token in \"Authorization: Bearer <token>\" header.\nYou can get it here: \"/auth/<username>\"");
        }
        return Ok().Text($"Hello, {auth!.Username}!");
    }

    [Get("/auth/{username}")]
    public static Result GetAuthToken(string username)
    {
        var token = JWT.CreateToken(new Credentials{  Username = username , Id = RandomNumberGenerator.GetInt32(0, Int32.MaxValue)});
        return Ok().Text(token);
    }
}
