namespace Celerio;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field)]
public class Length : Attribute
{
    public readonly int Min;
    public readonly int? Max;
    
    public Length(int max, int min = 0)
    {
        Min = min;
        Max = max;
    }
}

public class StringLengthValidation : ValidatorBase
{
    public override ArgumentValidator.Validator? CreateValidator(Type type, object[] attributes, string name)
    {
        var length = attributes.OfType<Length>().FirstOrDefault();
        if (length == null)
            return null;

        if (type == typeof(string))
        {
            if (length is { Max: not null, Min: 0 })
            {
                int max = length.Max.Value;
                string error = $"[{name}] length > {max}";
                return obj =>
                {
                    if ((obj as string)!.Length > max)
                        return new(false, error);
                    return new();
                };
            }

            if (length is { Max: not null, Min: not 0 })
            {
                int max = length.Max.Value;
                int min = length.Min;
                string errormax = $"[{name}] length > {max}";
                string errormin = $"[{name}] length < {min}";
                return obj =>
                {
                    var str = (obj as string)!;
                    if (str.Length > max)
                        return new(false, errormax);
                    if (str.Length < min)
                        return new(false, errormin);
                    return new();
                };
            }
            
            if (length is { Max: null, Min: not 0 })
            {
                int min = length.Min;
                string errormin = $"[{name}] length < {min}";
                return obj =>
                {
                    if ((obj as string)!.Length < min)
                        return new(false, errormin);
                    return new();
                };
            }
        }
        return null;
    }
}