using System.Security.Cryptography;
using System.Text;

namespace Celerio;

/// <summary>
/// Provides access to environment-specific configuration values used by the Celerio application.
/// These values are sourced from environment variables or provided defaults.
/// </summary>
public static class Environment
{
    /// <summary>
    /// Gets the secret key used for JWT token signing and HMAC validation.
    /// This key is derived from the CELERIO_AUTH environment variable;
    /// if not set, a hash of the current date and time is used as a fallback.
    /// </summary>
    public static readonly byte[] AUTH_SECRET = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(System.Environment.GetEnvironmentVariable("CELERIO_AUTH") ?? DateTime.Now.ToBinary().ToString()));

    /// <summary>
    /// Gets the port number on which the Celerio server listens for incoming requests.
    /// The value is retrieved from the CELERIO_PORT environment variable or defaults to 5000 if not specified.
    /// </summary>
    public static readonly int PORT = int.Parse(System.Environment.GetEnvironmentVariable("CELERIO_PORT") ?? "5000");
}
