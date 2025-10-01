using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;
using Celerio.Shared;

namespace CelerioSamples;

using Celerio;
using static Celerio.Result;


public static class Validators
{
    [Get("/checkUsername/{username}")]
    public static Result CheckUsername([Length(3,32)] [RegularExpression("^[a-zA-Z0-9]+$")] string username)
    {
        return Ok().Text(username);
    }

    [Get("/email")]
    public static Result Email([EmailAddress] string email)
    {
        return Ok().Text(email);
    }
    
    [Get("/url")]
    public static Result Url([Url] string url)
    {
        return TemporaryRedirect(url);
    }

    public class Person : IValidatable
    {
        public string Name { get; set; }
        public int Age { get; set; }

        public bool Validate(out string? reason)
        {
            if (string.IsNullOrEmpty(Name))
            {
                reason = "Name cannot be empty";
                return false;
            }
            if (Age < 0)
            {
                reason = "Age must be non-negative";
                return false;
            }

            if (Age < 18)
            {
                reason = "Person must be adult";
                return false;
            }

            reason = null;
            return true;
        }
    }

    [Post("/validators/validatable")]
    public static Result Person_Validation(Person person)
    {
        return Ok().Text($"Name: {person.Name}, Age: {person.Age}");
    }
}
