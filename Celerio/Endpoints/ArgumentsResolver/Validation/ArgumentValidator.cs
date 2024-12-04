using System.Reflection;

namespace Celerio;

public static class ArgumentValidator
{
    private readonly static List<ValidatorBase> _validators = new()
    {
        new ObjectValidation(),
        new StringLengthValidation()
    };
    
    public class ValidationResult
    {
        public bool Valid;
        public string? Message;

        public ValidationResult(bool valid = true, string? message = null)
        {
            Valid = valid;
            Message = message;
        }
    }

    public delegate ValidationResult Validator(object obj);
    
    public static Validator? CreateValidator(Type type, object[] attributes, string name)
    {
        List<Validator> validators = new List<Validator>();

        foreach (var val in _validators)
        {
            var result = val.CreateValidator(type, attributes, name);
            if(result != null)
                validators.Add(result);
        }
        
        if (validators.Count == 0)
            return null;
        if (validators.Count == 1)
            return validators[0];
        return CreateValidatorsChain(validators.ToArray());
    }
    
    internal static Validator CreateValidatorsChain(Validator[] validators)
    {
        return obj =>
        {
            foreach (var v in validators)
            {
                var result = v.Invoke(obj);
                if (!result.Valid)
                    return result;
            }

            return new ValidationResult();
        };
    }
}