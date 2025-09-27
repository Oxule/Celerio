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
        return Ok().Json(auth);
    }
    [Get("/auth/required")]
    public static Result AuthRequired(Credentials auth)
    {
        return Ok().Json(auth);
    }

    [Post("/auth")]
    public static Result AuthPost(Credentials body)
    {
        var token = JWT.CreateToken(body);
        return Ok().Text(token);
    }
}
