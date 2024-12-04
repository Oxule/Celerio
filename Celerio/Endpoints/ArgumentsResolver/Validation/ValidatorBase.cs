using System.Reflection;

namespace Celerio;

public class ValidatorBase
{
    public virtual ArgumentValidator.Validator? CreateValidator(Type type, object[] attributes, string name)
    {
        return null;
    }
}