using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace CelerioSamples;

using Celerio;
using static Celerio.Result;


public static class Validators
{
    [Get("/validators/string/length")]
    public static Result String_Length([Length(3,8)] string a)
    {
        return Ok().Text(a);
    }
    
    [Get("/validators/string/length/max")]
    public static Result String_Length_Max([MaxLength(5)] string a)
    {
        return Ok().Text(a);
    }
    [Get("/validators/string/length/min")]
    public static Result String_Length_Min([MinLength(3)] string a)
    {
        return Ok().Text(a);
    }

    [Get("/validators/int/range")]
    public static Result Int_Range([Range(0, 100)] int a)
    {
        return Ok().Text(a.ToString());
    }

    [Get("/validators/double/range")]
    public static Result Double_Range([Range(0.0, 100.0)] double a)
    {
        return Ok().Text(a.ToString());
    }

    [Get("/validators/float/range")]
    public static Result Float_Range([Range(0.0f, 100.0f)] float a)
    {
        return Ok().Text(a.ToString());
    }

    [Get("/validators/long/range")]
    public static Result Long_Range([Range(0L, 1000000L)] long a)
    {
        return Ok().Text(a.ToString());
    }

    [Get("/validators/string/regex")]
    public static Result String_Regex([RegularExpression("^[0-9]{3}-[0-9]{3}-[0-9]{4}$")] string phone)
    {
        return Ok().Text(phone);
    }

    [Get("/validators/string/email")]
    public static Result String_Email([EmailAddress] string email)
    {
        return Ok().Text(email);
    }

    [Get("/validators/string/url")]
    public static Result String_Url([Url] string url)
    {
        return Ok().Text(url);
    }
}
