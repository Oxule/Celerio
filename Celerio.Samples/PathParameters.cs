using System.Security.Cryptography;
using Celerio;
using static Celerio.Result;

namespace CelerioSamples;

public static class PathParameters
{
    [Get("/path/{text}")]
    public static Result Path(string text)
    {
        return Ok().Text(text);
    }
    [Get("/path/{text}/subpage")]
    public static Result SubPagePath(string text)
    {
        return Ok().Text($"Subpage of {text}");
    }
    
    [Get("/article/{id}")]
    public static Result Article(Guid id)
    {
        return Ok().Json(new {id, title = "Article title", content = "Article content"});
    }

    [Get("/acticle/{id}/likes")]
    public static Result ArticleLikes(Guid id)
    {
        return Ok().Text(RandomNumberGenerator.GetInt32(0,1000));
    }
    
    [Get("/acticle/{id}/comments/{commentId}")]
    public static Result ArticleComment(Guid id, Guid commentId)
    {
        return Ok().Json(new {id = commentId, articleId = id, text = "Comment text", username = "oxule"});
    }

    [Get("/*")]
    public static Result NotFoundPage()
    {
        return NotFound().Text("Not found custom page");
    }
}