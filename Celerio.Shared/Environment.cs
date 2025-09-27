using System.Security.Cryptography;
using System.Text;

namespace Celerio;

public static class Environment
{
    public static readonly byte[] AUTH_SECRET = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(System.Environment.GetEnvironmentVariable("CELERIO_AUTH") ?? DateTime.Now.ToBinary().ToString()));
    public static readonly int PORT = int.Parse(System.Environment.GetEnvironmentVariable("CELERIO_PORT") ?? "5000");
}