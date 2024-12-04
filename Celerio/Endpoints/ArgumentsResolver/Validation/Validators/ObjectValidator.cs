namespace Celerio;

public class ObjectValidation : ValidatorBase
{
    public override ArgumentValidator.Validator? CreateValidator(Type type, object[] attributes, string name)
    {
        if (!type.IsClass)
            return null;

        List<ArgumentValidator.Validator> fieldValidators = new ();
        
        foreach (var field in type.GetFields())
        {
            if(field.IsStatic)
                continue;
            var result = ArgumentValidator.CreateValidator(field.FieldType, field.GetCustomAttributes(true),
                name + "." + field.Name);
            
            if(result != null)
                fieldValidators.Add(obj=>result.Invoke(field.GetValue(obj)!));
        }
        
        if (fieldValidators.Count == 0)
            return null;
        if (fieldValidators.Count == 1)
            return fieldValidators[0];
        return ArgumentValidator.CreateValidatorsChain(fieldValidators.ToArray());
    }
}