using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;

/// <summary>
/// Utility helpers to create loopback NetworkStream pairs for unit testing.
/// </summary>
public static class NetworkStreamTestHelper
{
    private static readonly List<TcpClient> _serverKeepers = new List<TcpClient>();

    /// <summary>
    /// Creates a loopback connection and returns two <see cref="NetworkStream"/> instances:
    /// the first is the client-side stream and the second is the server-side stream.
    /// Both streams are connected to each other and can be used to simulate a real socket peer.
    /// </summary>
    /// <returns>
    /// A tuple (clientStream, serverStream). Caller is responsible for disposing the returned
    /// <see cref="NetworkStream"/> objects when they are no longer needed.
    /// </returns>
    /// <remarks>
    /// The server-side <see cref="TcpClient"/> is stored internally to prevent it from being
    /// garbage-collected and closing the connection unexpectedly during tests. Call
    /// <see cref="CleanupKeptServers"/> after your tests to free resources.
    /// </remarks>
    public static (NetworkStream clientStream, NetworkStream serverStream) CreateLoopbackNetworkStreams()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        try
        {
            var acceptTask = listener.AcceptTcpClientAsync();

            var client = new TcpClient();
            var localEndPoint = (IPEndPoint)listener.LocalEndpoint;
            client.Connect(localEndPoint.Address, localEndPoint.Port);

            var server = acceptTask.GetAwaiter().GetResult();

            listener.Stop();

            var clientStream = client.GetStream();
            var serverStream = server.GetStream();

            lock (_serverKeepers)
            {
                _serverKeepers.Add(server);
            }

            return (clientStream, serverStream);
        }
        catch
        {
            try
            {
                listener.Stop();
            }
            catch
            {
            }

            throw;
        }
    }

    /// <summary>
    /// Creates a loopback connection, writes the supplied bytes from the server-side stream,
    /// and returns the client-side <see cref="NetworkStream"/> from which those bytes can be read.
    /// </summary>
    /// <param name="data">Byte array to write into the server-side stream.</param>
    /// <returns>
    /// A <see cref="NetworkStream"/> on the client side which contains the supplied data as if
    /// it was sent from a remote peer.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is null.</exception>
    public static NetworkStream WriteNetworkStream(byte[] data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        var (clientStream, serverStream) = CreateLoopbackNetworkStreams();

        serverStream.Write(data, 0, data.Length);
        serverStream.Flush();

        return clientStream;
    }

    /// <summary>
    /// Releases and disposes all internally-kept server-side <see cref="TcpClient"/> instances.
    /// Call this method after tests complete to free resources.
    /// </summary>
    public static void CleanupKeptServers()
    {
        lock (_serverKeepers)
        {
            foreach (var s in _serverKeepers)
            {
                try
                {
                    s.Close();
                    s.Dispose();
                }
                catch
                {
                }
            }

            _serverKeepers.Clear();
        }
    }
}