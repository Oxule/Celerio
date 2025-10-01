using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace CelerioSamples;

using Celerio;
using static Celerio.Result;

public static class LinkShorter
{
    // Real database of course
    public static Dictionary<string, string> Links = new ();
    
    [Get("/short")]
    public static Result ShortLink([Url] string url)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(url));
        var code = Convert.ToBase64String(hash, 0, 6);
        if(!Links.ContainsKey(code))
            Links.Add(code, url);
        return Ok().Text(code);
    }
    [Get("/short/{code}")]
    public static Result RedirectLink(string code)
    {
        if (Links.TryGetValue(code, out var url))
        {
            return PermanentRedirect(url);
        }

        return NotFound();
    }
}