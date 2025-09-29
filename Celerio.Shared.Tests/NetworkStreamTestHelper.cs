using System;
using System.Net;
using System.Net.Sockets;
using System.IO;

/// <summary>
/// Small owner object that keeps both TcpClients alive and exposes their NetworkStreams.
/// Dispose() will clean up both sides.
/// </summary>
public sealed class LoopbackConnection : IDisposable
{
    private readonly TcpClient _client;
    private readonly TcpClient _server;
    private bool _disposed;

    public NetworkStream ClientStream { get; }
    public NetworkStream ServerStream { get; }

    internal LoopbackConnection(TcpClient client, TcpClient server)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _server = server ?? throw new ArgumentNullException(nameof(server));

        ClientStream = _client.GetStream();
        ServerStream = _server.GetStream();

        try { _client.NoDelay = true; _server.NoDelay = true; } catch { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try { ClientStream?.Dispose(); } catch { }
        try { ServerStream?.Dispose(); } catch { }

        try { _client?.Close(); _client?.Dispose(); } catch { }
        try { _server?.Close(); _server?.Dispose(); } catch { }
    }
}

/// <summary>
/// Helpers to create loopback NetworkStreams for tests.
/// Preferred usage: obtain a LoopbackConnection and dispose it when finished.
/// </summary>
public static class NetworkStreamTestHelper
{
    /// <summary>
    /// Create a loopback connection and return the owner object containing both streams.
    /// Caller is responsible for disposing the returned LoopbackConnection.
    /// </summary>
    public static LoopbackConnection CreateLoopbackConnection()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        try
        {
            var acceptTask = listener.AcceptTcpClientAsync();

            var client = new TcpClient();
            var local = (IPEndPoint)listener.LocalEndpoint;
            client.Connect(local.Address, local.Port);

            var server = acceptTask.GetAwaiter().GetResult();

            listener.Stop();

            return new LoopbackConnection(client, server);
        }
        catch
        {
            try { listener.Stop(); } catch { }
            throw;
        }
    }

    /// <summary>
    /// Create a loopback connection, write data from the client side and return the connection.
    /// Read the data from <see cref="LoopbackConnection.ServerStream"/>.
    /// </summary>
    public static LoopbackConnection WriteNetworkStream(byte[] data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        var conn = CreateLoopbackConnection();

        conn.ClientStream.Write(data, 0, data.Length);
        conn.ClientStream.Flush();

        return conn;
    }
}
