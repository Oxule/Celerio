namespace Celerio.Analyzers.Generators.Validators;

public static class PropValidators
{
    public static List<PropProvider> Registry = new List<PropProvider>()
    {
        DefaultPropValidators.StringLength,
        DefaultPropValidators.StringMinLength,
        DefaultPropValidators.StringMaxLength,
        DefaultPropValidators.IntRange,
        DefaultPropValidators.LongRange,
        DefaultPropValidators.ShortRange,
        DefaultPropValidators.ByteRange,
        DefaultPropValidators.SByteRange,
        DefaultPropValidators.UIntRange,
        DefaultPropValidators.ULongRange,
        DefaultPropValidators.UShortRange,
        DefaultPropValidators.FloatRange,
        DefaultPropValidators.DoubleRange,
        DefaultPropValidators.StringRegex,
        DefaultPropValidators.StringEmail,
        DefaultPropValidators.StringUrl
    };
}
