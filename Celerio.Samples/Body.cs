namespace CelerioSamples;

using Celerio;
using static Celerio.Result;

public static class Body
{
    public class PostArticleBody
    {
        public string Title { get; set; }
        public string Content { get; set; }
    }
    
    [Post("/article")]
    public static Result PostArticle(PostArticleBody body /*"body" is important*/)
    {
        return Ok().Json(body);
    }
}