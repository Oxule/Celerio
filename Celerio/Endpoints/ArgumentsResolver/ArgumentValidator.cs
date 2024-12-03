namespace Celerio;

public static class ArgumentValidator
{
    public class ValidationResult
    {
        public bool Valid;
    }

    public delegate ValidationResult Validator(object obj);

    public static Validator CreateValidator(Type type)
    {
        return obj => new ValidationResult();
    }
}