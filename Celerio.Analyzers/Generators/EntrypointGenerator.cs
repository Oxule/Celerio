namespace Celerio.Analyzers.Generators;

public static class EntrypointGenerator
{
    private const string Entrypoint = @"using System;
using System.Net;
using Celerio;
using Celerio.Generated;

public static partial class Program {
    public static async Task Main(string[] args) {
        var server = new Server(IPAddress.Any, 5000);
        server.Start();
        await Task.Delay(Timeout.Infinite);
    }
}
";
    
    public static string GenerateEntrypoint()
    {
        return Entrypoint;
    }
}