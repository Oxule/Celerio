namespace Celerio.Shared;

/// <summary>
/// Defines a contract for objects that require validation.
/// Implementing this interface allows objects to report their validity state.
/// </summary>
public interface IValidatable
{
    /// <summary>
    /// Validates the object and returns a boolean indicating whether the validation succeeded.
    /// If validation fails, an error reason is provided via the out parameter.
    /// </summary>
    /// <param name="reason">Outputs the validation failure reason, or null if valid.</param>
    /// <returns>True if the object is valid; otherwise, false.</returns>
    bool Validate(out string? reason);
}
