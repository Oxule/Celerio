namespace Celerio.Shared;

public interface IValidatable
{
    bool Validate(out string? reason);
}
